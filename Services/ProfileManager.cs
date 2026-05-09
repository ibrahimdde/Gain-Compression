using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FfmpegWrapper.Models;

namespace FfmpegWrapper.Services
{
    public class ProfileManager
    {
        private readonly string _filePath = "profiles.json";
        private List<CompressionProfile> _profiles;

        public ProfileManager()
        {
            _profiles = new List<CompressionProfile>();
            LoadProfiles();
        }

        public List<CompressionProfile> GetAllProfiles() => _profiles;

        public void AddProfile(CompressionProfile profile)
        {
            _profiles.Add(profile);
            SaveProfiles();
        }

        public void UpdateProfile(CompressionProfile updatedProfile)
        {
            var existing = _profiles.FirstOrDefault(p => p.Id == updatedProfile.Id);
            if (existing != null)
            {
                existing.ProfileName = updatedProfile.ProfileName;
                existing.Resolution = updatedProfile.Resolution;
                existing.Bitrate = updatedProfile.Bitrate;
                existing.Fps = updatedProfile.Fps;
                SaveProfiles();
            }
        }

        public void DeleteProfile(string id)
        {
            var profile = _profiles.FirstOrDefault(p => p.Id == id);
            if (profile != null)
            {
                _profiles.Remove(profile);
                SaveProfiles();
            }
        }

        private void LoadProfiles()
        {
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                _profiles = JsonSerializer.Deserialize<List<CompressionProfile>>(json) ?? new List<CompressionProfile>();
            }
            else
            {
                _profiles.Add(new CompressionProfile { ProfileName = "Varsayılan", Resolution = "1920x1080", Bitrate = 4000, Fps = 30 });
                _profiles.Add(new CompressionProfile { ProfileName = "Düşük Boyut", Resolution = "1280x720", Bitrate = 1500, Fps = 24 });
                SaveProfiles();
            }
        }

        private void SaveProfiles()
        {
            string json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
    }
}
