using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FfmpegWrapper.Models;

namespace FfmpegWrapper.Services
{
    // Bu sınıf, sıkıştırma profillerimizi (kalite ayarlarımızı) yönetmek (eklemek, silmek, kaydetmek) için var.
    public class ProfileManager
    {
        // Profilleri kaydedeceğimiz dosyanın adı.
        private readonly string _filePath = "profiles.json";
        
        // Tüm profillerimizi hafızada tutacağımız liste.
        private List<CompressionProfile> _profiles;

        // Kurucu Metot: Sınıf ilk çağrıldığında çalışır.
        public ProfileManager()
        {
            _profiles = new List<CompressionProfile>();
            LoadProfiles(); // Başlarken daha önceden kaydedilmiş profiller varsa onları yükle.
        }

        // Kaydettiğimiz tüm profilleri listelemek (getirmek) için bu metodu kullanıyoruz.
        // Öğrenci Notu: 'List<CompressionProfile>' diyerek geriye birden fazla profil döndüreceğimizi belirtiyoruz.
        public List<CompressionProfile> GetAllProfiles()
        {
            return _profiles;
        }

        // Yeni bir profil eklemek için.
        public void AddProfile(CompressionProfile yeniProfil)
        {
            _profiles.Add(yeniProfil); // Önce listeye ekle.
            SaveProfiles(); // Sonra dosyaya (profiles.json) kaydet ki program kapanınca silinmesin.
        }

        // Var olan bir profili düzenlemek (güncellemek) için bu metodu kullanıyoruz.
        public void UpdateProfile(CompressionProfile guncelProfil)
        {
            // Tüm profillerimizin içinde tek tek dönüyoruz.
            // Amacımız, güncellenmek istenen profili ID'sine bakarak bulmak.
            for (int i = 0; i < _profiles.Count; i++)
            {
                // Eğer listedeki profilin ID'si, bize gelen güncel profilin ID'si ile aynıysa, doğru profili bulduk demektir!
                if (_profiles[i].IdyiGetir() == guncelProfil.IdyiGetir())
                {
                    // Artık eski bilgilerin yerine yeni bilgileri yazabiliriz.
                    _profiles[i].ProfilAdi = guncelProfil.ProfilAdi;
                    _profiles[i].Cozunurluk = guncelProfil.Cozunurluk;
                    _profiles[i].Bitrate = guncelProfil.Bitrate;
                    _profiles[i].Fps = guncelProfil.Fps;
                    _profiles[i].VideoKodek = guncelProfil.VideoKodek;
                    _profiles[i].HizOnayari = guncelProfil.HizOnayari;
                    
                    SaveProfiles(); // Değişiklikleri dosyaya kaydetmeyi unutmuyoruz.
                    break; // Aradığımızı bulup güncellediğimiz için döngüye devam etmemize gerek yok.
                }
            }
        }

        // İstemediğimiz bir profili silmek için.
        public void DeleteProfile(string silinecekId)
        {
            for (int i = 0; i < _profiles.Count; i++)
            {
                if (_profiles[i].IdyiGetir() == silinecekId)
                {
                    _profiles.RemoveAt(i); // Numarasını bulduğumuz profili listeden çıkar.
                    SaveProfiles();
                    break; // Bulup sildiğimiz için döngüyü bitir.
                }
            }
        }

        // Kayıtlı profilleri dosyadan okuyan metot.
        private void LoadProfiles()
        {
            // Eğer "profiles.json" dosyası varsa içindekileri oku.
            if (File.Exists(_filePath))
            {
                string json = File.ReadAllText(_filePath);
                _profiles = JsonSerializer.Deserialize<List<CompressionProfile>>(json);
                
                // Eğer dosya boşsa ya da hatalıysa listeyi sıfırdan oluştur.
                if (_profiles == null)
                {
                    _profiles = new List<CompressionProfile>();
                }
            }
            else
            {
                // Eğer program ilk defa çalışıyorsa ve dosya yoksa, varsayılan (hazır) birkaç profil oluştur.
                _profiles.Add(new CompressionProfile { ProfilAdi = "Varsayılan", Cozunurluk = "1920x1080", Bitrate = 4000, Fps = 30, VideoKodek = "libx264", HizOnayari = "medium" });
                _profiles.Add(new CompressionProfile { ProfilAdi = "Düşük Boyut", Cozunurluk = "1280x720", Bitrate = 1500, Fps = 24, VideoKodek = "libx265", HizOnayari = "fast" });
                SaveProfiles(); // Oluşturduğumuz bu ilk profilleri dosyaya kaydet.
            }
        }

        // Listemizdeki son hali "profiles.json" dosyasına yazan metot.
        private void SaveProfiles()
        {
            // Verilerimizi (Listeyi) okunaklı bir JSON (metin) formatına çeviriyoruz.
            string json = JsonSerializer.Serialize(_profiles, new JsonSerializerOptions { WriteIndented = true });
            // Çevirdiğimiz bu metni dosyaya yazdırıyoruz.
            File.WriteAllText(_filePath, json);
        }
    }
}
