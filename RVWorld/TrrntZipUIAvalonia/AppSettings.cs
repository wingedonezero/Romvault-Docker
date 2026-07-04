using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TrrntZipUIAvalonia
{
    public static class AppSettings
    {
        private static readonly string SettingsPath;
        private static Dictionary<string, string> _settings;

        static AppSettings()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            SettingsPath = Path.Combine(appDir, "TrrntZipSettings.json");
            Load();
        }

        private static void Load()
        {
            _settings = new Dictionary<string, string>();
            try
            {
                if (System.IO.File.Exists(SettingsPath))
                {
                    string json = System.IO.File.ReadAllText(SettingsPath);
                    _settings = JsonSerializer.Deserialize<Dictionary<string, string>>(json)
                                ?? new Dictionary<string, string>();
                }
            }
            catch (Exception)
            {
                Console.WriteLine("Error reading app settings");
                _settings = new Dictionary<string, string>();
            }
        }

        private static void Save()
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_settings, options);
                System.IO.File.WriteAllText(SettingsPath, json);
            }
            catch (Exception)
            {
                Console.WriteLine("Error writing app settings");
            }
        }

        public static string ReadSetting(string key)
        {
            try
            {
                return _settings.TryGetValue(key, out string value) ? value : null;
            }
            catch (Exception)
            {
                Console.WriteLine("Error reading app settings");
                return null;
            }
        }

        public static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                _settings[key] = value;
                Save();
            }
            catch (Exception)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
    }
}
