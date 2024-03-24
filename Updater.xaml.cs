using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Cucubany;

public partial class Updater
{
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
        // Update logic
        
        LauncherUpdater updater = new LauncherUpdater("Zarinoow", "Cucubany", "bin/Release/net7.0-windows", AppDomain.CurrentDomain.BaseDirectory);
        await updater.DownloadUpdate();
        
        // Verify if a Update folder was created
        if (Directory.Exists(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Update")))
        {
            // Close the current application
            Close();
            
            // Execute updater.exe
            System.Diagnostics.Process.Start(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Updater.exe"));
            
        } else UpdateComplete();
        
        
    }
    
}