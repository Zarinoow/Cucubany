using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Cucubany;

public class FileDownloadStatus
{
    public string? FileName { get; set; }
    public DownloadAction Action { get; set; }
    public double Percentage { get; set; }
    
    public string GetStatus()
    {
        switch (Action)
        {
            case DownloadAction.Downloading:
                return "Téléchargement de " + FileName + "... " + Math.Round(Percentage) + "%";
            case DownloadAction.Verifying:
                return "Vérification de " + FileName + "... ";
            case DownloadAction.Updating:
                return "Mise à jour de " + FileName + "... " + Math.Round(Percentage) + "%";
            default:
                return "Action inconnue";
        }
    }
}

public enum DownloadAction
{
    Downloading,
    Verifying,
    Updating
}

public class GameFileDownloader
{
    private readonly HttpClient _httpClient;
    
    private readonly string _path;
    private readonly string _localPath;

    public GameFileDownloader(string path, string localPath)
    {
        _httpClient = new HttpClient();
        
        _path = path;
        _localPath = localPath;
    }
    
    public event Action<FileDownloadStatus> FileDownloadStatusChanged;

    public async Task DownloadFiles(bool verifyHash = true)
    {
        var url = $"{Updater.UpdaterUrl}/update/game/{_path}";
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CucubanyGameUpdater");

        var response = await _httpClient.GetStringAsync(url);
        var files = JArray.Parse(response);

        foreach (var file in files)
        {
            var fileName = (string)file["file"];
            var remoteFileSha = (string)file["hash"];
            var fileType = (string)file["type"];

            var localFilePath = $"{_localPath}/{fileName}";
            if (fileType == "dir")
            {
                var subDownloader = new GameFileDownloader($"{_path}/{fileName}", localFilePath);
                subDownloader.FileDownloadStatusChanged += (status) => FileDownloadStatusChanged?.Invoke(status);
                await subDownloader.DownloadFiles(verifyHash);
            }
            else
            {
                var status = new FileDownloadStatus { FileName = fileName, Action = DownloadAction.Verifying };
                FileDownloadStatusChanged?.Invoke(status);
                
                var downloadUrl = $"{Updater.UpdaterUrl}/download/game/{_path}/{fileName}";

                if (!File.Exists(localFilePath))
                {
                    status.Action = DownloadAction.Downloading;
                    await DownloadFile(downloadUrl, localFilePath, status);
                }
                else if (verifyHash && Updater.ComputeHash(localFilePath) != remoteFileSha)
                {
                    status.Action = DownloadAction.Updating;
                    await DownloadFile(downloadUrl, localFilePath, status);
                }
            }
        }
    }

    private async Task DownloadFile(string downloadUrl, string localFilePath, FileDownloadStatus status)
    {
        var directoryName = Path.GetDirectoryName(localFilePath);
        if (directoryName != null)
        {
            Directory.CreateDirectory(directoryName);
        }
        
        using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
        using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
        using (Stream streamToWriteTo = File.Open(localFilePath, FileMode.Create))
        {
            var total = response.Content.Headers.ContentLength ?? -1L;
            var canReportProgress = total != -1 && status != null;

            var totalRead = 0L;
            var buffer = new byte[8192];
            var isMoreToRead = true;

            do
            {
                var read = await streamToReadFrom.ReadAsync(buffer, 0, buffer.Length);
                if (read == 0)
                {
                    isMoreToRead = false;
                }
                else
                {
                    await streamToWriteTo.WriteAsync(buffer, 0, read);

                    totalRead += read;

                    if (canReportProgress)
                    {
                        status.Percentage = (totalRead * 1d) / (total * 1d) * 100;
                        FileDownloadStatusChanged?.Invoke(status);
                    }
                }
            }
            while (isMoreToRead);
        }
    }
}