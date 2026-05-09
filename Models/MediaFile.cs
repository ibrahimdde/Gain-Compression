using System.IO;

namespace FfmpegWrapper.Models
{
    // Ortak özellikleri taşıyan "Ana Sınıf" (Temel Sınıf)
    public class MediaFile
    {
        // Özellikleri (Alanları) herkesin erişebileceği şekilde public (açık) yapıyoruz.
        // Böylece kullanımı çok daha kolay oluyor.
        public string DosyaYolu;
        public string DosyaAdi;
        public string Uzanti;
        public long Boyut;

        // Kurucu Metot: Yeni dosya oluşturulurken dosya yolunu (nerede olduğunu) istiyoruz.
        public MediaFile(string yol)
        {
            // Dosya bilgisayarda gerçekten var mı diye kontrol ediyoruz.
            if (!File.Exists(yol))
            {
                throw new FileNotFoundException("Dosya bulunamadı.");
            }

            // Path sınıfı, bir dosya yolundan adı ve uzantıyı kolayca ayıklamamızı sağlar.
            DosyaYolu = yol;
            DosyaAdi = Path.GetFileNameWithoutExtension(yol);
            Uzanti = Path.GetExtension(yol);
            Boyut = new FileInfo(yol).Length; // Dosyanın bayt cinsinden büyüklüğü
        }

        // Medya dosyasının özetini dönen sanal (virtual) metot.
        // İsteyen sınıflar (örneğin VideoFile) bunu değiştirip kendine göre uyarlayabilir.
        public virtual string AciklamaGetir()
        {
            return $"Dosya: {DosyaAdi}";
        }
    }
}
