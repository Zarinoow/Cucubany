using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Cucubany.Launcher;
using Microsoft.VisualBasic.Devices;
using Microsoft.Win32;
using Keyboard = System.Windows.Input.Keyboard;

namespace Cucubany;

public partial class SettingsWindow : Window
{
    
    private CucubanyOptions gameOptions;
    
    public SettingsWindow()
    {
        InitializeComponent();
        
        // Obtenir les options de jeu
        gameOptions = LauncherMain.GetInstance().GetGameOptions();
        
        // Sauvegarder les options à la fermeture de la fenêtre
        this.Closing += SettingsWindow_Closing;
        
        /*
         * Adapter les paramètres de mémoire vive
         */

        // Obtenir la mémoire vive totale en gigaoctets
        double totalMemory = GetTotalMemoryInGB();

        // Définir la valeur Maximum de vos sliders
        MinMemorySlider.Maximum = totalMemory;
        MaxMemorySlider.Maximum = totalMemory;
        
        // Définir la valeur actuelle des sliders
        MinMemorySlider.Value = gameOptions.MinimumRamMb / 1024.0;
        MaxMemorySlider.Value = gameOptions.MaximumRamMb / 1024.0;
        
        /*
         * Charger les paramètres d'écran
         */
        
        // Définir les valeurs actuelles des TextBox
        WidthTextBox.Text = gameOptions.ScreenWidth.ToString();
        HeightTextBox.Text = gameOptions.ScreenHeight.ToString();
        
        // Définir la valeur de la case à cocher Plein écran
        FullScreenCheckBox.IsChecked = gameOptions.FullScreen;
        
        /*
         * Charger le chemin Java
         */
        
        // Définir le chemin Java actuel
        if(!string.IsNullOrEmpty(gameOptions.CustomJavaPath)) JavaPathText.Text = gameOptions.CustomJavaPath;
        
        // Activer la case à cocher si un chemin Java personnalisé est défini
        JavaPathCheckBox.IsChecked = gameOptions.IsCustomJavaPathEnabled() && !string.IsNullOrEmpty(gameOptions.CustomJavaPath);
    }
    
    private void SettingsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        gameOptions.MinimumRamMb = (int) (MinMemorySlider.Value * 1024);
        gameOptions.MaximumRamMb = (int) (MaxMemorySlider.Value * 1024);
        gameOptions.ScreenWidth = int.Parse(WidthTextBox.Text);
        gameOptions.ScreenHeight = int.Parse(HeightTextBox.Text);
        gameOptions.FullScreen = FullScreenCheckBox.IsChecked == true;
        gameOptions.Save();
    }
    
    /*
     * Navigation
     */
    private void closeButton_Click(object sender, RoutedEventArgs e)
    {
        this.Close();
    }
    
    private void DragWindow(object sender, MouseButtonEventArgs e)
    {
        Keyboard.ClearFocus();
        if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    /*
     * Mémoire vive
     */
    private double GetTotalMemoryInGB()
    {
        ComputerInfo CI = new ComputerInfo();
        ulong totalMemory = CI.TotalPhysicalMemory;

        // Convertissez la mémoire totale en gigaoctets et arrondissez à l'entier le plus proche
        return Math.Round(totalMemory / (1024.0 * 1024.0 * 1024.0));
    }

    private void MaxMemorySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if(e.NewValue < MinMemorySlider.Value)
        {
            MinMemorySlider.Value = e.NewValue;
        }
        MinMemorySlider.Maximum = e.NewValue;
    }
    
    /*
     * Chemin Java
     */
    private void ChangeJavaPath()
    {
        OpenFileDialog openFileDialog = new OpenFileDialog();
        openFileDialog.Filter = "Java Executable (*.exe)|*.exe";
        openFileDialog.Title = "Sélectionnez le chemin de Java";
        openFileDialog.InitialDirectory = "C:\\Program Files\\Java";
        if (openFileDialog.ShowDialog() == true)
        {
            gameOptions.CustomJavaPath = openFileDialog.FileName;
            JavaPathText.Text = gameOptions.CustomJavaPath;
        }
        else
        {
            ResetJavaPath();
        }
    }
    
    private void ResetJavaPath()
    {
        gameOptions.CustomJavaPath = "";
        gameOptions.EnableCustomJavaPath(false);
        JavaPathText.Text = "C:\\Program Files\\Java\\...\\bin\\javaw.exe";
        JavaPathCheckBox.IsChecked = false;
    }
    
    private void JavaPathGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if(JavaPathCheckBox.IsChecked == true)
        {
            ChangeJavaPath();
        }
    }
    
    private void JavaPathCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if(string.IsNullOrEmpty(gameOptions.CustomJavaPath)) ChangeJavaPath();

        // Activer le chemin Java s'il a bien été défini 
        if (string.IsNullOrEmpty(gameOptions.CustomJavaPath)) return;
        JavaPathTextBox.Fill = Brushes.LightGray;
        gameOptions.EnableCustomJavaPath(true);


    }

    private void JavaPathCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        JavaPathTextBox.Fill = Brushes.Gray;
        gameOptions.EnableCustomJavaPath(false);
    }
    
    /*
     * Taille de l'écran
     */
    
    private void ScreenSizeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
    {
        if (!int.TryParse(e.Text, out _))
        {
            e.Handled = true;
        }
        else
        {
            int number = int.Parse((sender as TextBox).Text + e.Text);
            if(number > 99999 | number <= 0) // 99999 is the maximum height
            {
                e.Handled = true;
            } 
        }
    }
    
    private void HeightTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        int minHeight = 480;
        if (HeightTextBox.Text == "")
        {
            HeightTextBox.Text = minHeight.ToString();
            return;
        }
        
        int height = int.Parse(HeightTextBox.Text);
        
        if (height < minHeight)
        {
            HeightTextBox.Text = minHeight.ToString();
        }
        else
        {
            double screenHeight = SystemParameters.PrimaryScreenHeight;
            if (height > screenHeight)
            {
                HeightTextBox.Text = screenHeight.ToString();
            }
        }
    }
    
    private void WidthTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        int minWidth = 854;
        if (WidthTextBox.Text == "")
        {
            WidthTextBox.Text = minWidth.ToString();
            return;
        }
        
        int width = int.Parse(WidthTextBox.Text);
        
        if (width < minWidth)
        {
            WidthTextBox.Text = minWidth.ToString();
        }
        else
        {
            double screenWidth = SystemParameters.PrimaryScreenWidth;
            if (width > screenWidth)
            {
                WidthTextBox.Text = screenWidth.ToString();
            }
        }
    }
    
    private void FullScreenCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        HeightTextBox.IsEnabled = false;
        WidthTextBox.IsEnabled = false;
    }
    
    private void FullScreenCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        HeightTextBox.IsEnabled = true;
        WidthTextBox.IsEnabled = true;
    }
}