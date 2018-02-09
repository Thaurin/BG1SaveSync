using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;

namespace BG1SaveSync.Classes
{
    public class AppFolder
    {
        public string AppFolderLocation;
        public string ConfigFileLocation => $"{AppFolderLocation}\\BG1SaveSync.ini";
        public string TempFolderLocation => $"{AppFolderLocation}\\Temp";
        public string LastError;
        public Dictionary<string, string> Config;
        public static Dictionary<string, string> DefaultConfig;

        public AppFolder()
        {
            AppFolderLocation = $"{Environment.GetFolderPath(Environment.SpecialFolder.Personal)}\\BG1SaveSync";
            string myDocuments = Environment.GetFolderPath(Environment.SpecialFolder.Personal);
            DefaultConfig = new Dictionary<string, string>();
            DefaultConfig.Add("SaveGameFolder", $"{myDocuments}\\Baldur's Gate - Enhanced Edition\\save");
            DefaultConfig.Add("SharedFolder", myDocuments.Substring(0, myDocuments.LastIndexOf("\\")) + "\\Dropbox\\Saves\\BG1");
            DefaultConfig.Add("MaxBackupFiles", "10");
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

            if (File.Exists(ConfigFileLocation))
            {
                Config = new Dictionary<string, string>();
                string[] configFile = File.ReadAllLines(ConfigFileLocation);

                foreach (string configLine in configFile)
                {
                    string trimmedLine = configLine.Trim();
                    if (trimmedLine == "" || trimmedLine.Substring(0, 1) == "#") continue;

                    string[] splitLine = trimmedLine.Split('=');
                    if (splitLine.Length != 2)
                    {
                        LastError = "Error reading configuration file.";
                        return false;
                    }

                    Config.Add(splitLine[0].Trim(), splitLine[1].Trim());
                }
            }
            else
            {
                // Create first time configuration
                Config = new Dictionary<string, string>();
                foreach (KeyValuePair<string, string> configItem in DefaultConfig)
                {
                    Config.Add(configItem.Key, configItem.Value);
                }
                WriteConfig();
            }

            return true;
        }

        public bool WriteConfig()
        {
            if (File.Exists(ConfigFileLocation))
            {
                try
                {
                    File.Delete(ConfigFileLocation);
                }
                catch (Exception ex)
                {
                    LastError = ex.Message;
                    return false;
                }
            }

            using (StreamWriter file = new StreamWriter(ConfigFileLocation))
            {
                file.WriteLine("# BG1SaveSync ini file. Blank lines are ignored.");

                foreach (KeyValuePair<string, string> configItem in Config)
                {
                    file.WriteLine($"{configItem.Key} = {configItem.Value}");
                }
            }

            return true;
        }
    }
}
