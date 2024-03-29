using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Cucubany;

public class LauncherUpdater
{
    private readonly HttpClient _httpClient;
    
    private readonly string _localPath;
    
    public bool IsUpdateCancelled { get; private set; } = false;
    private bool WritePermissionChecked { get; set; } = false;

    public LauncherUpdater(string localPath)
    {
        _httpClient = new HttpClient();
        
        _localPath = localPath;
    }

    public async Task DownloadUpdate(string path = "")
    {
        var url = $"{Updater.UpdaterUrl}/update/application/{path}";
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CucubanyApplicationUpdater");
        
        var response = await _httpClient.GetStringAsync(url);
        var files = JArray.Parse(response);
    
        foreach (var file in files)
        {
            if(IsUpdateCancelled) return;
            
            var fileName = (string)file["file"];
            var remoteFileSha = (string)file["hash"];
            var fileType = (string)file["type"];
    
            var localFilePath = Path.Combine(_localPath, path, fileName);
            var updateFilePath = Path.Combine(_localPath, "Update", path, fileName);
    
            if (fileType == "dir")
            {
                await DownloadUpdate(Path.Combine(path, fileName));
            }
            else
            {
                var downloadUrl = $"{Updater.UpdaterUrl}/download/application/{path}/{fileName}";
                if (File.Exists(localFilePath))
                {
                    var localFileSha = Updater.ComputeHash(localFilePath);
                    if (localFileSha != remoteFileSha)
                    {
                        Console.WriteLine($"Updating file: {fileName}");
                        await DownloadFile(downloadUrl, updateFilePath);
                    }
                }
                else
                {
                    Console.WriteLine($"Downloading new file: {fileName}");
                    await DownloadFile(downloadUrl, updateFilePath);
                }
            }
        }
    }
    
    private async Task DownloadFile(string downloadUrl, string filePath)
    {
        var directoryName = Path.GetDirectoryName(filePath);
        
        if(!WritePermissionChecked)
        {
            if(!HasWritePermission(directoryName))
            {
                Console.WriteLine($"No write permission for {filePath}");
                IsUpdateCancelled = true;
                return;
            }
            WritePermissionChecked = true;
        }
        
        if (directoryName != null)
        {
            Directory.CreateDirectory(directoryName);
        }
    
        using (var response = await _httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead))
        using (var streamToReadFrom = await response.Content.ReadAsStreamAsync())
        using (Stream streamToWriteTo = File.Open(filePath, FileMode.Create))
        {
            await streamToReadFrom.CopyToAsync(streamToWriteTo);
        }
    }
    
    private bool HasWritePermission(string folderPath)
    {
        try
        {
            // Check if the directory exists. If not, create it.
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }

            // Use a temporary file name. You can change this to something more specific if needed.
            var tmpFileName = Path.Combine(folderPath, Path.GetRandomFileName());
            using (File.Create(tmpFileName, 1, FileOptions.DeleteOnClose))
            {
                // If we can create a file, that means we have write access.
                return true;
            }
        }
        catch
        {
            // If an exception occurs, that means we do not have write access.
            return false;
        }
    }
}