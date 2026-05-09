using System;

namespace FfmpegWrapper.Models
{
    public class CompressionProfile
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string ProfileName { get; set; }
        public string Resolution { get; set; } // Örn: 1920x1080
        public int Bitrate { get; set; } // Örn: 2000 (kbps)
        public int Fps { get; set; } // Örn: 30

        // Encapsulation (Kapsülleme) örneği: Profil geçerliliğini kontrol etme
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ProfileName) && Bitrate > 0 && Fps > 0;
        }
    }
}
