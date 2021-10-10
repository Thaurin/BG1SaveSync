using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BG1SaveSync.Classes
{
    public class SaveGame
    {
        public string Name { get; set; }
        public string ZipName { get; set; }
        public DateTime Date;
        public string DateString => $"{Date.ToShortDateString()}";
        public string TimeString => $"{Date.ToShortTimeString()}";

        public SaveGame(DirectoryInfo dirInfo)
        {
            Name = dirInfo.Name;
            ZipName = $"{Name}.bg2save";
            Date = dirInfo.CreationTime;
        }

        public SaveGame(FileInfo fileInfo)
        {
            ZipName = fileInfo.Name;
            Name = ZipName.Substring(0, ZipName.Length - ".bg2save".Length);
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
                FileInfo[] saveFiles = new DirectoryInfo(directory).GetFiles("*.bg2save").OrderByDescending(p => p.CreationTime).ToArray();
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
