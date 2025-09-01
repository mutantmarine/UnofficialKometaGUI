using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using KometaGUIv3.Shared.Models;
using Newtonsoft.Json;

namespace KometaGUIv3.Shared.Services
{
    public class ProfileManager
    {
        private static readonly string ProfilesDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "KometaGUIv3", "Profiles");
        private List<KometaProfile> _profiles;

        public ProfileManager()
        {
            _profiles = new List<KometaProfile>();
            EnsureProfilesDirectoryExists();
            LoadProfiles();
        }

        private void EnsureProfilesDirectoryExists()
        {
            if (!Directory.Exists(ProfilesDirectory))
            {
                Directory.CreateDirectory(ProfilesDirectory);
            }
        }

        public List<KometaProfile> GetAllProfiles()
        {
            return _profiles.ToList();
        }

        public KometaProfile CreateProfile(string name)
        {
            if (ProfileExists(name))
            {
                throw new InvalidOperationException($"Profile '{name}' already exists.");
            }

            var profile = new KometaProfile
            {
                Name = name,
                CreatedDate = DateTime.Now,
                LastModified = DateTime.Now
            };

            _profiles.Add(profile);
            SaveProfile(profile);
            return profile;
        }

        public void SaveProfile(KometaProfile profile)
        {
            profile.LastModified = DateTime.Now;
            var profilePath = GetProfilePath(profile.Name);
            var json = JsonConvert.SerializeObject(profile, Formatting.Indented);
            File.WriteAllText(profilePath, json);
        }

        public KometaProfile LoadProfile(string name)
        {
            var profilePath = GetProfilePath(name);
            if (!File.Exists(profilePath))
            {
                return null;
            }

            var json = File.ReadAllText(profilePath);
            return JsonConvert.DeserializeObject<KometaProfile>(json);
        }

        public void DeleteProfile(string name)
        {
            var profilePath = GetProfilePath(name);
            if (File.Exists(profilePath))
            {
                File.Delete(profilePath);
            }

            _profiles.RemoveAll(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        public bool ProfileExists(string name)
        {
            return _profiles.Any(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
        }

        private void LoadProfiles()
        {
            _profiles.Clear();
            if (!Directory.Exists(ProfilesDirectory))
                return;

            var profileFiles = Directory.GetFiles(ProfilesDirectory, "*.json");
            foreach (var file in profileFiles)
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var profile = JsonConvert.DeserializeObject<KometaProfile>(json);
                    if (profile != null)
                    {
                        _profiles.Add(profile);
                    }
                }
                catch (Exception ex)
                {
                    // Log error but continue loading other profiles
                    Console.WriteLine($"Error loading profile {file}: {ex.Message}");
                }
            }
        }

        private string GetProfilePath(string name)
        {
            var safeFileName = string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
            return Path.Combine(ProfilesDirectory, $"{safeFileName}.json");
        }

        public List<string> GetProfileNames()
        {
            return _profiles.Select(p => p.Name).ToList();
        }
    }
}