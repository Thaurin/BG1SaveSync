using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace BG1SaveSync
{
    public class SaveGame
    {
        public string FullName { get; set; }
        public string Name { get; set; }
        public DateTime Date;
        public string DateTimeString => $"{Date.ToShortDateString()} {Date.ToShortTimeString()}";

        public SaveGame(DirectoryInfo dirInfo)
        {
            FullName = dirInfo.Name;
            Name = FullName.Substring(FullName.LastIndexOf('\\') + 11);
            Date = dirInfo.CreationTime;
        }

        public SaveGame(FileInfo fileInfo)
        {
            FullName = fileInfo.Name;
            Name = FullName.Substring(0, FullName.Length - ".bg1save".Length);
            Date = fileInfo.CreationTime;
        }

        public static List<SaveGame> GetSaveGamesFromSaveGameDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                List<SaveGame> saveGameList = new List<SaveGame>();
                DirectoryInfo[] saveDirs = new DirectoryInfo(directory).GetDirectories().OrderByDescending(p => p.CreationTime).ToArray();
                foreach (DirectoryInfo dirInfo in saveDirs)
                {
                    saveGameList.Add(new SaveGame(dirInfo));
                }

                return saveGameList;
            }
            else
            {
                return null;
            }
        }

        public static List<SaveGame> GetSaveGamesFromSharedDirectory(string directory)
        {
            if (Directory.Exists(directory))
            {
                List<SaveGame> saveGameList = new List<SaveGame>();
                FileInfo[] saveFiles = new DirectoryInfo(directory).GetFiles("*.bg1save").OrderByDescending(p => p.CreationTime).ToArray();
                foreach (FileInfo file in saveFiles)
                {
                    saveGameList.Add(new SaveGame(file));
                }

                return saveGameList;
            }
            else
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            SaveDirTextBox.Text = $"{myDocuments}\\Baldur's Gate - Enhanced Edition\\save";
            SharedDirTextBox.Text = myDocuments.Substring(0, myDocuments.LastIndexOf("\\")) + "\\Dropbox\\Saves\\BG1";
            RescanSaveGames();
        }

        private void RescanSaveGames()
        {
            cmbSaves.ItemsSource = FromSaveRadio.IsChecked == true ?
                SaveGame.GetSaveGamesFromSaveGameDirectory(SaveDirTextBox.Text) :
                SaveGame.GetSaveGamesFromSharedDirectory(SharedDirTextBox.Text);
            cmbSaves.SelectedIndex = 0;
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
                    }
                    else
                    {
                        SharedDirTextBox.Text = fbd.SelectedPath;
                    }

                    RescanSaveGames();
                }
            }
        }

        private void DirectionRadio_Click(object sender, RoutedEventArgs e)
        {
            RescanSaveGames();
        }

        private void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSaves.Items.Count == 0)
            {
                MessageBox.Show("No save games found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveGame selectedSaveGame = (SaveGame)cmbSaves.SelectedValue;
            string source, destination;

            if (FromSaveRadio.IsChecked == true)
            {
                source = $"{SaveDirTextBox.Text}\\{selectedSaveGame.FullName}";
                destination = $"{SharedDirTextBox.Text}\\{selectedSaveGame.Name}.bg1save";
            }
            else
            {
                source = $"{SharedDirTextBox.Text}\\{selectedSaveGame.FullName}";
                destination = $"{SaveDirTextBox.Text}\\000000000-{selectedSaveGame.Name}";
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
                        const int maxBackupFiles = 10;
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
