using System;
using System.Linq;
using System.Net.Http;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Installer.Forge;
using CmlLib.Core.Installer.Forge.Versions;
using Cucubany.Launcher;

namespace Cucubany;

public class LauncherMain
{
    private static LauncherMain _instance = null!;
    
    private readonly MainWindow _mainWindow;

    private readonly CucubanyPath _path;
    private readonly CucubanyOptions _gameOptions;
    private readonly CMLauncher _launcher;
    
    private readonly JELoginHandler _loginHandler;
    private MSession? _session;
    
    private readonly ModManager _modManager;

    public LauncherMain(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _instance = this;
        _path = new CucubanyPath();
        _gameOptions = new CucubanyOptions(_path);
        _loginHandler = new JELoginHandlerBuilder()
            .WithAccountManager(System.IO.Path.Combine(_path.BasePath, "launcher_accounts.json"))
            .Build();
        _launcher = new CMLauncher(_path);
        _modManager = new ModManager(this);
    }
    
    public async void Launch()
    {
        System.Net.ServicePointManager.DefaultConnectionLimit = 256;
        
        _mainWindow.EnablePlayButton(false);
        _mainWindow.ShowStatus();
        
        _launcher.FileChanged += (e) =>
        {
            SetStatus($"Vérification de Minecraft ({e.FileKind})... {Math.Round(Math.Round(e.ProgressedFileCount / (double)e.TotalFileCount) * 100)}%");
        };
        
        
        await _modManager.DownloadAndVerifyMods();

        GitHubFileDownloader forceDownload = new GitHubFileDownloader("Zarinoow", "Cucubany", "Game/Download", _path.BasePath);
        forceDownload.FileDownloadStatusChanged += (e) => SetStatus(e.GetStatus());
        await forceDownload.DownloadFiles();
        
        GitHubFileDownloader onceDownload = new GitHubFileDownloader("Zarinoow", "Cucubany", "Game/DownloadOnce", _path.BasePath);
        onceDownload.FileDownloadStatusChanged += (e) => SetStatus(e.GetStatus());
        await onceDownload.DownloadFiles(false);
        
        var versionLoader = new ForgeVersionLoader(new HttpClient());
        var versions = await versionLoader.GetForgeVersions("1.16.5");
        var recommendedVersion = versions.First(v => v.IsRecommendedVersion);

        var forge = new MForge(_launcher);
        forge.FileChanged += (e) => SetStatus($"Vérification de Forge ({e.FileKind})... {Math.Round((double) e.ProgressedFileCount / e.TotalFileCount * 100)}%");
        // ~~~ event handlers ~~~
        var versionName = await forge.Install(recommendedVersion.MinecraftVersionName, recommendedVersion.ForgeVersionName);
        // ~~~ launch codes ~~~
        var process = await _launcher.CreateProcessAsync(versionName, _gameOptions);
        process.Start();
        
        _mainWindow.Close();
    }
    
    public void SetStatus(String status)
    {
        _mainWindow.UpdateStatus(status);
    }
    
    public static LauncherMain GetInstance()
    {
        return _instance;
    }
    
    public MSession? GetSession()
    {
        return _session;
    }
    
    public CucubanyPath GetPath()
    {
        return _path;
    }
    
    public CucubanyOptions GetGameOptions()
    {
        return _gameOptions;
    }
    
    public void ReconnectAccount()
    {
        var accounts = _loginHandler.AccountManager.GetAccounts();
        if (accounts.Count == 0) return;
        if (string.IsNullOrEmpty(_gameOptions.LastConnectedAccount)) return;
        
        var account = accounts.GetJEAccountByUsername(_gameOptions.LastConnectedAccount);
        
        
        try
        {
            _session = account.ToLauncherSession();
        }
        catch (Exception)
        {
            return;
        }
        OnLoginCompleted();
        _gameOptions.UpdateSession();
        
    }
    
    public async void ShowLoginWindow()
    {
        _mainWindow.EnablePlayButton(false);

        try
        {
            _session = await _loginHandler.AuthenticateInteractively();
        }
        catch (Exception e)
        {
            _mainWindow.UpdateAccountInfo();
            if (e.GetType().FullName == "XboxAuthNet.OAuth.CodeFlow.AuthCodeException") return;
            System.Windows.MessageBox.Show($"Une erreur est survenue lors de la connexion: {e.Message}", "Impossible de se connecter", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            return;
        }
        
        OnLoginCompleted();
        _gameOptions.UpdateSession();
    }
    
    private void OnLoginCompleted()
    {
        _gameOptions.LastConnectedAccount = _session?.Username;
        _gameOptions.Save();
        _mainWindow.UpdateAccountInfo();
    }


}