using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CmlLib.Core.Auth;

namespace Cucubany
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private const String ApiAvatarUrl = "https://mc-heads.net/avatar/";

        public MainWindow()
        {
            InitializeComponent();

            var launcherMain = new LauncherMain(this);
            launcherMain.ReconnectAccount();
        }
        
        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            // Obtenez la position de la souris par rapport à la fenêtre
            Point mousePosition = e.GetPosition(this);
            int paralaxForce = 30;

            // Calculez la distance de la souris par rapport au centre de la fenêtre
            double deltaX = (Width / 2 - mousePosition.X) / paralaxForce;
            double deltaY = (Height / 2 - mousePosition.Y) / paralaxForce;

            // Déplacez le logo en fonction de la position de la souris
            Logo.Margin = new Thickness(deltaX, deltaY, 0, 0);
        }
        
        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }

        private void PlayButton_Click(object sender, RoutedEventArgs e)
        {
            LauncherMain.GetInstance().Launch();
        }
        
        private void minimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }
        
        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }
        
        private void connectAccount_Click(object sender, RoutedEventArgs e)
        {
            LauncherMain.GetInstance().ShowLoginWindow();
        }
        
        public void UpdateAccountInfo()
        {
            MSession? session = LauncherMain.GetInstance().GetSession();
            
            if (session != null)
            {
                userPicture.ImageSource = new BitmapImage(new Uri(ApiAvatarUrl + session.Username));
                userName.Text = session.Username;
                EnablePlayButton(true);
            }
            else
            {
                userPicture.ImageSource = new BitmapImage(new Uri(ApiAvatarUrl + "MHF_Steve"));
                userName.Text = "Non connecté";
                EnablePlayButton(false);
            }
        }
        
        public void UpdateStatus(String status)
        {
            Dispatcher.Invoke(() =>
            {
                StatusLabel.Text = status;
            });
        }
        
        public void ShowStatus()
        {
            StatusLabel.Visibility = Visibility.Visible;
        }
        
        public void EnablePlayButton(bool enabled)
        {
            if (enabled)
            {
                PlayButton.IsEnabled = true;
            }
            else
            {
                PlayButton.IsEnabled = false;
            }
        }
    }
}