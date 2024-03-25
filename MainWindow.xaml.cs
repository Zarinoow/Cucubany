using System;
using System.Collections.Generic;
using System.Linq;
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
            
            KeyDown += MainWindow_KeyDown; // Konami code

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
        
        /*
         * Konami code
         */
        public static bool KonamiCodeEnabled = false;
        
        private readonly List<Key> _keysPressed = new List<Key>();
        private readonly List<Key> _konamiCode = new List<Key>
        {
            Key.Up, Key.Up, Key.Down, Key.Down, Key.Left, Key.Right, Key.Left, Key.Right, Key.B, Key.A
        };

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            _keysPressed.Add(e.Key);

            if (_keysPressed.Count > _konamiCode.Count)
            {
                _keysPressed.RemoveAt(0);
            }

            if (_keysPressed.SequenceEqual(_konamiCode))
            {
                KonamiCodeActivated();
            }
        }

        private void KonamiCodeActivated()
        {
            KonamiCodeEnabled = !KonamiCodeEnabled;
            if(KonamiCodeEnabled)
            {
                MessageBox.Show("Le mode développeur est désormais activé ! Le mode développeur est une fonctionnalité expérimentale qui peut causer des problèmes. Utilisez à vos risques et périls.", "Mode développeur activé", MessageBoxButton.OK, MessageBoxImage.Warning);
            } else
            {
                MessageBox.Show("Vous venez de désactiver le mode développeur. Les fonctionnalités expérimentales ont été désactivées.", "Mode développeur désactivé", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            LauncherMain.GetInstance().GetGameOptions().Save();
        }
        
    }
}