using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using BG1SaveSync.Classes;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace BG1SaveSync
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon notifyIcon;
        private AppFolder appFolder;

        protected override void OnStateChanged(EventArgs e)
        {
            if (WindowState == WindowState.Minimized)
            {
                this.Hide();
            }
            base.OnStateChanged(e);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            //monitoringThread.Abort();
            notifyIcon.Icon = null;
            notifyIcon.Dispose();
            base.OnClosing(e);
        }

        public MainWindow()
        {
            appFolder = new AppFolder();
            if (!appFolder.Initialize())
            {
                MessageBox.Show(appFolder.LastError, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
                return;
            }

            // Handle system tray icon
            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = new System.Drawing.Icon("BG1SaveSync.ico");
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += delegate (object sender, EventArgs args)
            {
                if (WindowState == WindowState.Normal)
                {
                    WindowState = WindowState.Minimized;
                }
                else
                {
                    Show();
                    WindowState = WindowState.Normal;
                }
            };

            // Monitor game process
            var task = Task.Run(() =>
            {
                do
                {
                    Process[] processes = Process.GetProcessesByName("baldur");
                    if (processes.Length > 0)
                    {
                        processes[0].WaitForExit();
                        Dispatcher.Invoke((Action)(() => BrintToFront()));
                    }
                    Thread.Sleep(10000);
                } while (true);
            });

            InitializeComponent();
            SaveDirTextBox.Text = appFolder.Config["SaveGameFolder"];
            SharedDirTextBox.Text = appFolder.Config["SharedFolder"];
            RescanSaveFolder();
        }

        private void BrintToFront()
        {
            if (!IsVisible)
            {
                Show();
            }

            if (WindowState == WindowState.Minimized)
            {
                WindowState = WindowState.Normal;
            }

            Activate();
            Topmost = true;
            Topmost = false;
            Focus();
        }

        private void RescanSaveFolder()
        {
            SavesCombo.ItemsSource = FromSaveRadio.IsChecked == true ?
                SaveGame.GetSaveGamesFromSaveGameDirectory(SaveDirTextBox.Text) :
                SaveGame.GetSaveGamesFromSharedDirectory(SharedDirTextBox.Text);
            SavesCombo.SelectedIndex = 0;
        }

        private void RescanSelectedSaveGame()
        {
            if (SavesCombo.SelectedValue != null)
            {
                string saveGameLocation;
                SaveGame selectedSaveGame = (SaveGame)SavesCombo.SelectedValue;

                if (FromSaveRadio.IsChecked == true)
                {
                    saveGameLocation = $"{SaveDirTextBox.Text}\\{selectedSaveGame.Name}";

                    if (!Directory.Exists(saveGameLocation))
                    {
                        MessageBox.Show("Save game directory no longer exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        RescanSaveFolder();
                        return;
                    }
                }
                else
                {
                    saveGameLocation = $"{appFolder.TempFolderLocation}\\{selectedSaveGame.Name}";

                    // Make sure that there is no trash lying around
                    if (Directory.Exists(saveGameLocation))
                    {
                        try
                        {
                            Directory.Delete(saveGameLocation, recursive: true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }

                    if (!File.Exists($"{SharedDirTextBox.Text}\\{selectedSaveGame.ZipName}"))
                    {
                        MessageBox.Show("Save game no longer exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        RescanSaveFolder();
                        return;
                    }

                    Directory.CreateDirectory(saveGameLocation);
                    ZipFile.ExtractToDirectory($"{SharedDirTextBox.Text}\\{selectedSaveGame.ZipName}", saveGameLocation);
                }

                ImagePanel.Children.Clear();

                // Screenshot
                string screenShotFileName = $"{saveGameLocation}\\BALDUR.bmp";
                if (File.Exists(screenShotFileName))
                {
                    BitmapImage bmImage = new BitmapImage();
                    bmImage.BeginInit();
                    bmImage.UriSource = new Uri(screenShotFileName);
                    bmImage.CacheOption = BitmapCacheOption.OnLoad;
                    bmImage.EndInit();

                    ImagePanel.Children.Add(new Image
                    {
                        Source = bmImage,
                        Margin = new Thickness(0, 0, 15, 0),
                        Stretch = Stretch.None
                    });
                }

                // Portraits
                int i = -1;
                bool fileExists = true; // Assume the existence of the first portrait
                while (i++ < 6 && fileExists == true)
                {
                    string fileName = $"{saveGameLocation}\\PORTRT{i}.bmp";
                    fileExists = File.Exists(fileName);

                    if (fileExists)
                    {
                        BitmapImage bmImage = new BitmapImage();
                        bmImage.BeginInit();
                        bmImage.UriSource = new Uri(fileName);
                        bmImage.CacheOption = BitmapCacheOption.OnLoad;
                        bmImage.EndInit();

                        ImagePanel.Children.Add(new Image
                        {
                            Source = bmImage,
                            Margin = new Thickness(0, 0, 5, 0),
                            Stretch = Stretch.None
                        });
                    }
                }

                // Clean up
                if (FromSaveRadio.IsChecked != true)
                {
                    if (Directory.Exists(saveGameLocation))
                    {
                        try
                        {
                            Directory.Delete(saveGameLocation, recursive: true);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                    }
                }
            }
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Button senderTextBox = (Button)sender;
            var fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = senderTextBox.Name == "SaveDirBrowseButton" ? SaveDirTextBox.Text : SharedDirTextBox.Text
            };

            using (fbd)
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    if (senderTextBox.Name == "SaveDirBrowseButton")
                    {
                        SaveDirTextBox.Text = fbd.SelectedPath;
                        appFolder.Config["SaveGameFolder"] = fbd.SelectedPath;
                    }
                    else
                    {
                        SharedDirTextBox.Text = fbd.SelectedPath;
                        appFolder.Config["SharedFolder"] = fbd.SelectedPath;
                    }

                    appFolder.WriteConfig();
                    RescanSaveFolder();
                }
            }
        }

        private void SavesCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            RescanSelectedSaveGame();
        }

        private void DirectionRadio_Click(object sender, RoutedEventArgs e)
        {
            RescanSaveFolder();
        }

        private void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (SavesCombo.Items.Count == 0)
            {
                MessageBox.Show("No save games found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveGame selectedSaveGame = (SaveGame)SavesCombo.SelectedValue;
            string source, destination;

            if (FromSaveRadio.IsChecked == true)
            {
                source = $"{SaveDirTextBox.Text}\\{selectedSaveGame.Name}";
                destination = $"{SharedDirTextBox.Text}\\{selectedSaveGame.ZipName}";
            }
            else
            {
                source = $"{SharedDirTextBox.Text}\\{selectedSaveGame.ZipName}";
                destination = $"{SaveDirTextBox.Text}\\{selectedSaveGame.Name}";
            }

            bool destinationExists = FromSaveRadio.IsChecked == true ? File.Exists(destination) : Directory.Exists(destination);
            if (destinationExists)
            {
                string destinationBase = destination.Substring(destination.LastIndexOf("\\") + 1);

                MessageBoxResult result = MessageBox.Show(
                    $"Save {destinationBase} already exists! Replace with selected save? (a backup will be saved in the shared folder)",
                    "Save already exists", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                switch (result)
                {
                    case MessageBoxResult.Yes:
                        int maxBackupFiles = Convert.ToInt16(appFolder.Config["MaxBackupFiles"]);
                        int i = 1;
                        bool moved = false;
                        while (i <= maxBackupFiles && moved == false)
                        {
                            string backupLocation = $"{SharedDirTextBox.Text}\\{destinationBase}.backup-{i++}";
                            bool backupExists = FromSaveRadio.IsChecked == true ? File.Exists(backupLocation) : Directory.Exists(backupLocation);
                            if (!backupExists)
                            {
                                try
                                {
                                    if (FromSaveRadio.IsChecked == true)
                                    {
                                        File.Move(destination, backupLocation);
                                    }
                                    else
                                    {
                                        Directory.Move(destination, backupLocation);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                                    return;
                                }

                                moved = true;
                            }
                        }

                        if (moved == false)
                        {
                            MessageBox.Show("Backup of save game failed (too many backup files). Aborted. ", "Aborted!", MessageBoxButton.OK, MessageBoxImage.Information);
                            return;
                        }
                        break;
                    case MessageBoxResult.No:
                        MessageBox.Show("Transfer cancelled.", "Aborted!", MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                }
            }

            try
            {
                if (FromSaveRadio.IsChecked == true)
                {
                    ZipFile.CreateFromDirectory(source, destination);
                }
                else
                {
                    Directory.CreateDirectory(destination);
                    ZipFile.ExtractToDirectory(source, destination);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Save game has been transfered to {destination}.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
