namespace Cucubany;

using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class FileDownloadStatus
{
    public string FileName { get; set; }
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

public class GitHubFileDownloader
{
    private readonly HttpClient _httpClient;
    
    private readonly string _user;
    private readonly string _repo;
    private readonly string _path;
    private readonly string _localPath;

    public GitHubFileDownloader(string user, string repo, string path, string localPath)
    {
        _httpClient = new HttpClient();
        
        _user = user;
        _repo = repo;
        _path = path;
        _localPath = localPath;
    }
    
    public event Action<FileDownloadStatus> FileDownloadStatusChanged;

    public async Task DownloadFiles(bool verifyHash = true)
    {
        var url = $"https://api.github.com/repos/{_user}/{_repo}/contents/{_path}";
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Cucubany-Updater"); // GitHub API needs a user-agent

        var response = await _httpClient.GetStringAsync(url);
        var files = JArray.Parse(response);

        foreach (var file in files)
        {
            var fileName = (string)file["name"];
            var downloadUrl = (string)file["download_url"];
            var remoteFileSha = (string)file["sha"];
            var fileType = (string)file["type"];

            var localFilePath = $"{_localPath}/{fileName}";
            if (fileType == "dir")
            {
                var subDownloader = new GitHubFileDownloader(_user, _repo, $"{_path}/{fileName}", localFilePath);
                subDownloader.FileDownloadStatusChanged += (status) => FileDownloadStatusChanged?.Invoke(status);
                await subDownloader.DownloadFiles(verifyHash);
            }
            else
            {
                var status = new FileDownloadStatus { FileName = fileName, Action = DownloadAction.Verifying };
                FileDownloadStatusChanged?.Invoke(status);

                if (!File.Exists(localFilePath))
                {
                    status.Action = DownloadAction.Downloading;
                    await DownloadFile(downloadUrl, localFilePath, status);
                }
                else if (verifyHash && ComputeSha1(localFilePath) != remoteFileSha)
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

    public static string ComputeSha1(string filePath)
    {
        using (var stream = File.OpenRead(filePath))
        {
            var contentBytes = File.ReadAllBytes(filePath);
            var size = contentBytes.Length;
            var header = $"blob {size}\0";
            var headerBytes = Encoding.UTF8.GetBytes(header);
            var blob = new byte[headerBytes.Length + contentBytes.Length];

            Buffer.BlockCopy(headerBytes, 0, blob, 0, headerBytes.Length);
            Buffer.BlockCopy(contentBytes, 0, blob, headerBytes.Length, contentBytes.Length);

            using (var sha = new SHA1Managed())
            {
                byte[] checksum = sha.ComputeHash(blob);
                return BitConverter.ToString(checksum).Replace("-", String.Empty).ToLower();
            }
        }
    }
}