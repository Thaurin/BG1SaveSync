using System;
using System.IO;

namespace BG1SaveSync.Classes
{
    public class AppFolder
    {
        public string AppFolderLocation;
        public string TempFolderLocation => $"{AppFolderLocation}\\Temp";
        public string LastError;

        public AppFolder()
        {
            AppFolderLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}\\BG1SaveSync";
        }

        public bool Initialize()
        {
            if (!Directory.Exists(AppFolderLocation))
            {
                try
                {
                    Directory.CreateDirectory(AppFolderLocation);
                    Directory.CreateDirectory(TempFolderLocation);
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return false;
                }
            }

            return true;
        }
    }
}
