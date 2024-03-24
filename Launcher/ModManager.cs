using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Cucubany.Launcher;

public class ModManager {
    private const String GistUrl = "https://gist.githubusercontent.com/Zarinoow/6cd26b57fb4cb4a54075e925946b0e9d/raw/";

    private readonly LauncherMain _main; 
    
    public ModManager(LauncherMain main)
    {
        _main = main;
    }
    
    public async Task DownloadAndVerifyMods()
    {
        var modsDir = Path.Combine(_main.GetPath().BasePath, "mods");
        
        if (!Directory.Exists(modsDir))
        {
            Directory.CreateDirectory(modsDir);
        }
        
        var mods = await GetModsFromGist();

        foreach (var mod in mods)
        {
            _main.SetStatus($"Vérification de {mod.Name}...");
            var modPath = Path.Combine(modsDir, mod.FileName);
            
            if (File.Exists(modPath))
            {
                var data = await File.ReadAllBytesAsync(modPath);
                var hash = ComputeHash(data);
                if (hash == mod.ExpectedHash)
                {
                    continue;
                }
                
                _main.SetStatus($"Mise à jour de {mod.Name}...");
            } 
            else _main.SetStatus($"Téléchargement de {mod.Name}...");

            using (var client = new HttpClient())
            {
                using (var response = await client.GetAsync(mod.DownloadUrl, HttpCompletionOption.ResponseHeadersRead))
                {
                    using (var fileStream = File.Create(modPath))
                    {
                        using (var downloadStream = await response.Content.ReadAsStreamAsync())
                        {
                            var totalBytes = response.Content.Headers.ContentLength.Value;
                            var bytesDownloaded = 0L;
                            var buffer = new byte[8192];
                            var bytesRead = 0;

                            while ((bytesRead = await downloadStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                bytesDownloaded += bytesRead;
                                var percentage = Math.Round((double)bytesDownloaded / totalBytes * 100);
                                _main.SetStatus($"Téléchargement de {mod.Name}... {percentage}%");
                            }
                        }
                    }
                }
            }
        }

        var existingMods = Directory.GetFiles(modsDir);
        foreach (var mod in existingMods)
        {
            if (mods.All(m => Path.Combine(modsDir, m.FileName) != mod))
            {
                File.Delete(mod);
            }
        }
    }
    
    private async Task<List<Mod>> GetModsFromGist()
    {
        using (var client = new HttpClient())
        {
            var response = await client.GetStringAsync(GistUrl);
            var mods = JsonConvert.DeserializeObject<List<Mod>>(response);
            return mods;
        }
    }

    private string ComputeHash(byte[] data)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(data);
            return BitConverter.ToString(hashBytes).Replace("-", "");
        }
    }
}