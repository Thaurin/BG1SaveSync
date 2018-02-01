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
        public string PrettyName => FullName.Substring(FullName.LastIndexOf('\\') + 11);
        public DateTime Date;
        public string DateTimeString => $"{Date.ToShortDateString()} {Date.ToShortTimeString()}";

        public SaveGame(DirectoryInfo dirInfo)
        {
            FullName = dirInfo.Name;
            Date = dirInfo.CreationTime;
        }

        public static List<SaveGame> GetSaveGamesFromDirectory(string directory)
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
            DestDirTextBox.Text = myDocuments.Substring(0, myDocuments.LastIndexOf("\\")) + "\\Dropbox\\Saves\\BG1";
            cmbSaves.ItemsSource = SaveGame.GetSaveGamesFromDirectory(SaveDirTextBox.Text);
            cmbSaves.SelectedIndex = 0;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Button senderTextBox = (Button)sender;
            var fbd = new System.Windows.Forms.FolderBrowserDialog()
            {
                SelectedPath = senderTextBox.Name == "SaveDirBrowseButton" ? SaveDirTextBox.Text : DestDirTextBox.Text
            };

            using (fbd)
            {
                System.Windows.Forms.DialogResult result = fbd.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    if (senderTextBox.Name == "SaveDirBrowseButton")
                    {
                        SaveDirTextBox.Text = fbd.SelectedPath;
                        cmbSaves.ItemsSource = SaveGame.GetSaveGamesFromDirectory(SaveDirTextBox.Text);
                        cmbSaves.SelectedIndex = 0;
                    }
                    else
                    {
                        DestDirTextBox.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (cmbSaves.Items.Count == 0)
            {
                MessageBox.Show("No save games in save folder!", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            SaveGame selectedSaveGame = (SaveGame)cmbSaves.SelectedValue;
            string saveDir = $"{SaveDirTextBox.Text}\\{selectedSaveGame.FullName}";
            string destFile = $"{DestDirTextBox.Text}\\{selectedSaveGame.PrettyName}.bg1save";

            try
            {
                ZipFile.CreateFromDirectory(saveDir, destFile);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            MessageBox.Show($"Save game has been exported to {destFile}.", "Success!", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
