using System;

namespace Cucubany;

using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class LauncherUpdater
{
    private readonly HttpClient _httpClient;
    
    private readonly string _user;
    private readonly string _repo;
    private readonly string _path;
    private readonly string _localPath;

    public LauncherUpdater(string user, string repo, string path, string localPath)
    {
        _httpClient = new HttpClient();
        
        _user = user;
        _repo = repo;
        _path = path;
        _localPath = localPath;
    }

    public async Task DownloadUpdate(string path = "")
    {
        var url = $"https://api.github.com/repos/{_user}/{_repo}/contents/{_path}/{path}";
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("Cucubany-Updater"); // GitHub API needs a user-agent
    
        var response = await _httpClient.GetStringAsync(url);
        var files = JArray.Parse(response);
    
        foreach (var file in files)
        {
            var fileName = (string)file["name"];
            var downloadUrl = (string)file["download_url"];
            var remoteFileSha = (string)file["sha"];
            var fileType = (string)file["type"];
    
            var localFilePath = Path.Combine(_localPath, path, fileName);
            var updateFilePath = Path.Combine(_localPath, "Update", path, fileName);
    
            if (fileType == "dir")
            {
                await DownloadUpdate(Path.Combine(path, fileName));
            }
            else
            {
                if (File.Exists(localFilePath))
                {
                    var localFileSha = GitHubFileDownloader.ComputeSha1(localFilePath);
                    if (localFileSha != remoteFileSha)
                    {
                        await DownloadFile(downloadUrl, updateFilePath);
                    }
                }
                else
                {
                    await DownloadFile(downloadUrl, updateFilePath);
                }
            }
        }
    }
    
    private async Task DownloadFile(string downloadUrl, string filePath)
    {
        var directoryName = Path.GetDirectoryName(filePath);
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
}