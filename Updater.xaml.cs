using System;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;

namespace Cucubany;

public partial class Updater
{
    public static string UpdaterUrl = "";
    private const string UpdaterGistUrl = "https://gist.githubusercontent.com/Zarinoow/0a0db05bf965d3c0c4a055e716ee01c1/raw/";
    
    public Updater()
    {
        InitializeComponent();
        Loaded += (s, e) => Update();
    }

    private void UpdateComplete()
    {
        MainWindow window = new MainWindow();
        window.Show();
        Close();
    }
    
    private async void Update() 
    {
        // Find the updater URL
        using (var client = new HttpClient())
        {
            var response = await client.GetAsync(UpdaterGistUrl);
            UpdaterUrl = await response.Content.ReadAsStringAsync();
        }
        
        // Update logic
        
        LauncherUpdater updater = new LauncherUpdater(AppDomain.CurrentDomain.BaseDirectory);
        await updater.DownloadUpdate();
        
        // Verify if a Update folder was created
        /*if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update")))
        {
            // Close the current application
           Close();
            
            // Execute updater.exe
            System.Diagnostics.Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe"));
            
        } else UpdateComplete();
        */
        UpdateComplete(); // REMOVE FOR PRODUCTION
    }
    
    public static string ComputeHash(string filePath)
    {
        using (var sha256 = SHA256.Create())
        {
            var hashBytes = sha256.ComputeHash(File.ReadAllBytes(filePath));
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }
    
}