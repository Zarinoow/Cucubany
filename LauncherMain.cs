using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using CmlLib.Core;
using CmlLib.Core.Auth;
using CmlLib.Core.Auth.Microsoft;
using CmlLib.Core.Auth.Microsoft.Sessions;
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

    public LauncherMain(MainWindow mainWindow)
    {
        _mainWindow = mainWindow;
        _instance = this;
        _path = new CucubanyPath();
        _gameOptions = new CucubanyOptions(_path);
        _loginHandler = new JELoginHandlerBuilder()
            .WithAccountManager(Path.Combine(_path.BasePath, "launcher_accounts.json"))
            .Build();
        _launcher = new CMLauncher(_path);
    }
    
    public async void Launch()
    {
        ServicePointManager.DefaultConnectionLimit = 256;
        
        _mainWindow.EnablePlayButton(false);
        _mainWindow.ShowStatus();
        
        _launcher.FileChanged += (e) =>
        {
            SetStatus($"Vérification de Minecraft ({e.FileKind})... {Math.Round(e.ProgressedFileCount / (double) e.TotalFileCount * 100)}%");
        };

        if (!MainWindow.KonamiCodeEnabled)
        {
            // Vérification des fichiers de jeu obligatoires
            GameFileDownloader requiredDownload = new GameFileDownloader("required", _path.BasePath);
            requiredDownload.FileDownloadStatusChanged += (e) => SetStatus(e.GetStatus());
            await requiredDownload.DownloadFiles();
        
            // Vérification des fichiers de jeu facultatifs
            GameFileDownloader onceDownload = new GameFileDownloader("optional", _path.BasePath);
            onceDownload.FileDownloadStatusChanged += (e) => SetStatus(e.GetStatus());
            await onceDownload.DownloadFiles(false);
        
            // Nettoyage des fichiers inutiles
            SetStatus("Nettoyage...");
            GameFileCleaner cleaner = new GameFileCleaner("mods", _path.BasePath + "/mods");
            await cleaner.CleanFiles();
        }
        
        // Vérification et installation de Forge
        var versionLoader = new ForgeVersionLoader(new HttpClient());
        var versions = await versionLoader.GetForgeVersions("1.18.2");
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
    
    public async void ReconnectAccount()
    {
        var accounts = _loginHandler.AccountManager.GetAccounts();
        if (accounts.Count == 0) return;
        if (string.IsNullOrEmpty(_gameOptions.LastConnectedAccount)) return;

        JEGameAccount account;
        
        try
        {
            account = accounts.GetJEAccountByUsername(_gameOptions.LastConnectedAccount);
        }
        catch (Exception e)
        {
            await Task.Run(() => MessageBox.Show("Votre compte n'a pas pu être reconnecté automatiquement. Veuillez essayer de vous reconnecter manuellement.", "Impossible de se reconnecter", MessageBoxButton.OK, MessageBoxImage.Error));
            return;
        }
        
        try
        {
            _session = await _loginHandler.Authenticate(account);
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
            MessageBox.Show($"Une erreur est survenue lors de la connexion: {e.Message}", "Impossible de se connecter", MessageBoxButton.OK, MessageBoxImage.Error);
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