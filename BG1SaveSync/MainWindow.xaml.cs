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
            Date = fileInfo.LastWriteTime;
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
            cmbSaves.ItemsSource = SaveGame.GetSaveGamesFromSaveGameDirectory(SaveDirTextBox.Text);
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
                        cmbSaves.ItemsSource = SaveGame.GetSaveGamesFromSaveGameDirectory(SaveDirTextBox.Text);
                        cmbSaves.SelectedIndex = 0;
                    }
                    else
                    {
                        SharedDirTextBox.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private void DirectionRadio_Click(object sender, RoutedEventArgs e)
        {
            RadioButton radioButton = (RadioButton)sender;
            if (radioButton.Name == "FromSaveRadio")
            {
                cmbSaves.ItemsSource = SaveGame.GetSaveGamesFromSaveGameDirectory(SaveDirTextBox.Text);
                cmbSaves.SelectedIndex = 0;
            }
            else
            {
                cmbSaves.ItemsSource = SaveGame.GetSaveGamesFromSharedDirectory(SharedDirTextBox.Text);
                cmbSaves.SelectedIndex = 0;
            }
        }

        private void TransferButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSaves.Items.Count == 0)
            {
                MessageBox.Show("No save games found!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveGame selectedSaveGame = (SaveGame)cmbSaves.SelectedValue;
            string saveDir, saveFile;

            if (FromSaveRadio.IsChecked == true)
            {
                saveDir = $"{SaveDirTextBox.Text}\\{selectedSaveGame.FullName}";
                saveFile = $"{SharedDirTextBox.Text}\\{selectedSaveGame.Name}.bg1save";

                try
                {
                    ZipFile.CreateFromDirectory(saveDir, saveFile);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"Save game has been exported to {saveFile}.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                saveDir = $"{SaveDirTextBox.Text}\\000000000-{selectedSaveGame.Name}";
                saveFile = $"{SharedDirTextBox.Text}\\{selectedSaveGame.FullName}";

                if (Directory.Exists(saveDir))
                {
                    MessageBox.Show($"Save {selectedSaveGame.Name} already exists!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                Directory.CreateDirectory(saveDir);

                try
                {
                    ZipFile.ExtractToDirectory(saveFile, saveDir);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                MessageBox.Show($"Save game has been imported to {saveDir}.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
            }

        }
    }
}
