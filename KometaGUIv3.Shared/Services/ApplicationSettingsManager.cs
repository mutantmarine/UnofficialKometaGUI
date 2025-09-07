using System;
using System.IO;
using Newtonsoft.Json;

namespace KometaGUIv3.Shared.Services
{
    public class ApplicationSettings
    {
        public bool HasShownWelcome { get; set; } = false;
        public DateTime FirstRunDate { get; set; } = DateTime.Now;
        public DateTime LastAccessDate { get; set; } = DateTime.Now;
    }

    public class ApplicationSettingsManager
    {
        private static readonly string AppDataDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KometaGUIv3");
        private static readonly string SettingsFilePath = Path.Combine(AppDataDirectory, "app-settings.json");
        private ApplicationSettings _settings;

        public ApplicationSettingsManager()
        {
            EnsureAppDataDirectoryExists();
            LoadSettings();
        }

        private void EnsureAppDataDirectoryExists()
        {
            if (!Directory.Exists(AppDataDirectory))
            {
                Directory.CreateDirectory(AppDataDirectory);
            }
        }

        public ApplicationSettings GetSettings()
        {
            return _settings;
        }

        public bool HasShownWelcome()
        {
            return _settings.HasShownWelcome;
        }

        public void MarkWelcomeShown()
        {
            _settings.HasShownWelcome = true;
            _settings.LastAccessDate = DateTime.Now;
            SaveSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(SettingsFilePath))
                {
                    var json = File.ReadAllText(SettingsFilePath);
                    _settings = JsonConvert.DeserializeObject<ApplicationSettings>(json) ?? new ApplicationSettings();
                }
                else
                {
                    _settings = new ApplicationSettings();
                    SaveSettings(); // Create the file on first run
                }
            }
            catch (Exception ex)
            {
                // If there's any error loading settings, start fresh
                _settings = new ApplicationSettings();
                SaveSettings();
            }

            // Update last access date
            _settings.LastAccessDate = DateTime.Now;
        }

        private void SaveSettings()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_settings, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (Exception ex)
            {
                // Log error but don't crash the application
                Console.WriteLine($"Error saving application settings: {ex.Message}");
            }
        }

        public void UpdateLastAccessDate()
        {
            _settings.LastAccessDate = DateTime.Now;
            SaveSettings();
        }
    }
}