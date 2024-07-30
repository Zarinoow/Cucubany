using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;

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

        if (updater.IsUpdateCancelled)
        {
            Spinner.Visibility = Visibility.Hidden;
            UpdateText.Text = "Erreur lors de la mise à jour !";
            UpdateText.Foreground = Brushes.Red;

            MessageBoxResult result = MessageBoxResult.None;
            
            switch (updater.ExitCode)
            {
                // Restart with admin rights
                case 1:
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = AppDomain.CurrentDomain.BaseDirectory + "Cucubany.exe",
                        Verb = "runas",
                        UseShellExecute = true
                    };
                    Process.Start(startInfo);
                    break;
                // Server error
                case 2:
                    result = MessageBox.Show("Une erreur est survenue lors du contact avec le serveur de mise à jour. L'utilisation du programme sera limitée.\n\nSouhaitez-vous continuer ?", "Défaillance générale du serveur de mise à jour", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    break;
                // Bad API response
                case 3:
                    result = MessageBox.Show("Une erreur est survenue lors de l'interprétation des informations de mise à jour. L'utilisation du programme sera limitée.\n\nSouhaitez-vous continuer ?", "Communication impossible avec le serveur de mise à jour", MessageBoxButton.YesNo, MessageBoxImage.Error);
                    break;
            }
            
            if(result == MessageBoxResult.None || result == MessageBoxResult.No) Close();
            if (result == MessageBoxResult.Yes)
            {
                MainWindow.OfflineMode = true;
                UpdateComplete();
            }
            return;

            
        }
        
        // Verify if a Update folder was created
        if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update")))
        {
            // Close the current application
           Close();
            
            // Execute updater.exe
            System.Diagnostics.Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe"));
            
        } else UpdateComplete();
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