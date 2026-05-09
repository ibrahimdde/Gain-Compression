using System;

namespace FfmpegWrapper.Models
{
    // Bu sınıf, video sıkıştırma ayarlarını (profilini) tutmak için bir şablondur.
    public class CompressionProfile
    {
        // ÖĞRENCİ NOTU: Encapsulation (Kapsülleme) Örneği
        // Sadece ID (kimlik) değişkenini gizli (private) yapıyoruz.
        // Dışarıdan doğrudan değiştirilemez, sadece bizim yazdığımız metotla okunabilir.
        private string gizliId;

        // Herkesin erişebileceği (public) normal değişkenlerimiz (Alanlar / Properties)
        // WPF ekranlarının bunları görebilmesi için { get; set; } ekliyoruz.
        public string ProfilAdi { get; set; }
        public string Cozunurluk { get; set; }
        public int Bitrate { get; set; } // Veri akım hızı
        public int Fps { get; set; } // Saniyedeki kare hızı
        public string VideoKodek { get; set; } // H264, H265, AV1
        public string HizOnayari { get; set; } // fast, slow vb.

        // Kurucu Metot: Yeni bir profil oluşturulduğunda ilk çalışan kod.
        public CompressionProfile()
        {
            // Yeni oluşturulan her profile otomatik olarak benzersiz bir kimlik veriyoruz.
            gizliId = Guid.NewGuid().ToString();
        }

        // Gizli olan ID'yi dışarıdan okumak için bir metot yazıyoruz.
        // Dışarıdan kimse ID'yi değiştiremez, sadece okuyabilir. (Kapsülleme amacı budur)
        public string IdyiGetir()
        {
            return gizliId;
        }

        // Girilen değerlerin mantıklı olup olmadığını kontrol eden basit bir metot.
        public bool GecerliMi()
        {
            // Eğer profil adı boş değilse ve diğer sayılar 0'dan büyükse her şey yolunda demektir.
            if (ProfilAdi != "" && Bitrate > 0 && Fps > 0 && VideoKodek != "" && HizOnayari != "")
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}

