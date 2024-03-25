using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Cucubany;

public class GameFileCleaner
{
    private readonly HttpClient _httpClient;
    
    private readonly string _path;
    private readonly string _localPath;

    public GameFileCleaner(string path, string localPath)
    {
        _httpClient = new HttpClient();
        
        _path = path;
        _localPath = localPath;
    }
    
    public async Task CleanFiles()
    {
        var url = $"{Updater.UpdaterUrl}/update/game/required/{_path}";
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "CucubanyGameCleaner");
        
        var response = await _httpClient.GetStringAsync(url);
        var files = JArray.Parse(response);
        
        // Get all files in the local directory
        var localFiles = Directory.GetFiles(_localPath);

        foreach (var localFile in localFiles)
        {
            var fileName = Path.GetFileName(localFile);

            // Check if the local file is in the list of required files
            var isRequired = files.Any(file => file["file"].ToString() == fileName);

            // If the local file is not required, delete it
            if (!isRequired)
            {
                File.Delete(localFile);
            }
        }
        
    }
}