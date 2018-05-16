using System;
using System.Configuration;
using System.IO;
using System.Security.Permissions;
using System.Security.AccessControl;
using System.Security.Principal;

namespace STARS.Applications.VETS.Plugins.RDEImportTool
{
    public static class Config
    {
        public static bool AskToRunExe { get; private set; }
        public static bool ShowCase { get; private set; }
        public static string LogFilePath { get; private set; }
        public static string RDEToolPath { get; private set; }
        public static string LastOpenedFilePath { get { return lastOpenedFilePath; } set { SetField("LastOpenedFilePath", value); } }
        private static string lastOpenedFilePath;

        private static void UpdateFields()
        {
            AskToRunExe = TypeCast.ToBool(AppConfig("AskToRunExe"));
            ShowCase = TypeCast.ToBool(AppConfig("ShowCase"));
            LogFilePath = AppConfig("LogFilePath");
            RDEToolPath = AppConfig("RDEToolPath");
            lastOpenedFilePath = AppConfig("LastOpenedFilePath");
        }

        [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
        static Config()
        {
            string path = typeof(Config).Assembly.Location + ".config";
            string dir = Path.GetDirectoryName(path);
            string file = Path.GetFileName(path);

            FileSystemWatcher watcher = new FileSystemWatcher();
            watcher.Path = dir;
            watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            watcher.Filter = file;
            watcher.Changed += new FileSystemEventHandler(OnAppConfigChanged);
            watcher.EnableRaisingEvents = true;

            UpdateFields();
        }

        private static void OnAppConfigChanged(object source, FileSystemEventArgs e)
        {
            try
            {
                UpdateFields();
            }
            catch (Exception ex)
            {
                if (!ex.Message.Contains("being used by another process"))
                {
                    throw ex;
                }
            }
        }

        private static string FormatPath(string path)
        {
            if (!path.EndsWith(@"\"))
            {
                return path + @"\";
            }
            return path;
        }

        private static string AppConfig(string key)
        {
            Configuration config = null;
            string exeConfigPath = typeof(Config).Assembly.Location;
            config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            if (config == null || config.AppSettings.Settings.Count == 0)
            {
                throw new Exception(String.Format("Config file {0}.config is missing or could not be loaded.", exeConfigPath));
            }

            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element != null)
            {
                string value = element.Value;
                if (!string.IsNullOrEmpty(value))
                    return value;
            }
            return string.Empty;
        }

        private static void SetField(string key, string value)
        {
            Configuration config = null;
            string exeConfigPath = typeof(Config).Assembly.Location;
            config = ConfigurationManager.OpenExeConfiguration(exeConfigPath);
            if (config == null || config.AppSettings.Settings.Count == 0)
            {
                throw new Exception(String.Format("Config file {0}.config is missing or could not be loaded.", exeConfigPath));
            }

            KeyValueConfigurationElement element = config.AppSettings.Settings[key];
            if (element == null) return;
            element.Value = value;
            config.Save();

            UpdateFields();
        }

    }
}
