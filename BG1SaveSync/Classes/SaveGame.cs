using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BG1SaveSync.Classes
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
}
