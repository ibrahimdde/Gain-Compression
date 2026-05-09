using System;

namespace FfmpegWrapper.Models
{
    public class CompressionProfile
    {
        // 1. Sınıf seviyesi klasik Kapsülleme (Encapsulation) - Gizli (Private) Alanlar
        private string _id;
        private string _profileName;
        private string _resolution;
        private int _bitrate;
        private int _fps;

        // Kurucu Metot (Constructor)
        public CompressionProfile()
        {
            _id = Guid.NewGuid().ToString();
        }

        // Açık (Public) Erişim Metotları (Getter ve Setter)
        public string Id
        {
            get { return _id; }
            set { _id = value; }
        }

        public string ProfileName
        {
            get { return _profileName; }
            set { _profileName = value; }
        }

        public string Resolution
        {
            get { return _resolution; }
            set { _resolution = value; }
        }

        public int Bitrate
        {
            get { return _bitrate; }
            set { _bitrate = value; }
        }

        public int Fps
        {
            get { return _fps; }
            set { _fps = value; }
        }

        // Encapsulation (Kapsülleme) örneği: Profil geçerliliğini kontrol etme
        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(ProfileName) && Bitrate > 0 && Fps > 0;
        }
    }
}
