using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FfmpegWrapper.Exceptions;
using FfmpegWrapper.Models;

namespace FfmpegWrapper.Services
{
    // Bu sınıf arka planda FFmpeg (video işleme motoru) programını çalıştırıp videolarımızı sıkıştırmamızı sağlar.
    public class FfmpegEngine
    {
        // FFmpeg programının bilgisayardaki adresi.
        private readonly string _ffmpegYolu = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");

        // Ekrana (Arayüze) haber göndermek için olaylar (Event) tanımlıyoruz.
        // Action<double, string>: Yüzde kaç bittiğini (double) ve geçen süreyi (string) göndereceğiz.
        public event Action<double, string> İlerlemeDurumuDegistiginde;
        // Action<string>: Arkada çalışan FFmpeg'den gelen mesajları (logları) ekrana basmak için göndereceğiz.
        public event Action<string> BilgiMesajiGeldiginde;

        // Videoyu sıkıştıran ana metot. İşlemin bitmesini bekleyebilmek için (donmaması için) "async Task" yapıyoruz.
        public async Task<string> VideoyuSikistirAsync(VideoFile secilenVideo, string kaydedilecekKlasor, CompressionProfile ayarlar)
        {
            // Yeni dosyanın adını belirliyoruz. (Örnek: video1_compressed.mp4)
            string yeniDosyaAdi = $"{secilenVideo.DosyaAdi}_compressed{secilenVideo.Uzanti}";
            string yeniDosyaYolu = Path.Combine(kaydedilecekKlasor, yeniDosyaAdi);

            // Eğer aynı isimde bir dosya zaten varsa, ismin sonuna (1), (2) gibi rakamlar ekliyoruz ki eskisini silmeyelim.
            int sayac = 1;
            while (File.Exists(yeniDosyaYolu))
            {
                yeniDosyaAdi = $"{secilenVideo.DosyaAdi}_compressed({sayac}){secilenVideo.Uzanti}";
                yeniDosyaYolu = Path.Combine(kaydedilecekKlasor, yeniDosyaAdi);
                sayac++;
            }

            // FFmpeg programına vereceğimiz komutları hazırlıyoruz.
            // -y: Üzerine yazma uyarısı sorma
            // -i: Giriş dosyası (İşlenecek video)
            string komutlar = $"-y -i \"{secilenVideo.DosyaYolu}\" ";
            
            // Eğer çözünürlük belirtilmişse (Örnek: 1920x1080), komuta çözünürlük değiştirme kodunu ekle.
            if (!string.IsNullOrEmpty(ayarlar.Cozunurluk) && ayarlar.Cozunurluk.Contains("x"))
            {
                komutlar += $"-vf scale={ayarlar.Cozunurluk.Replace("x", ":")} ";
            }

            // -b:v: Videonun saniyelik veri boyutu (Bitrate)
            // -r: Saniyedeki kare hızı (FPS)
            // -c:v: Video Kodeği (H264, H265, AV1)
            // -preset: Kodlama hızı ayarı (fast, slow)
            komutlar += $"-b:v {ayarlar.Bitrate}k -r {ayarlar.Fps} -c:v {ayarlar.VideoKodek} -preset {ayarlar.HizOnayari} \"{yeniDosyaYolu}\"";

            // Hazırladığımız komutlarla FFmpeg'i çalıştır ve bitmesini bekle.
            await FfmpegCalistirAsync(komutlar);
            
            // İşlem bittiğinde yeni videonun yerini geri döndür.
            return yeniDosyaYolu;
        }

        // Asıl siyah ekran (konsol) komutlarını çalıştıran yardımcı metot.
        private async Task FfmpegCalistirAsync(string komutlar)
        {
            // Process (İşlem), bilgisayarda arkada bir program çalıştırmamızı sağlar.
            using (Process islem = new Process())
            {
                islem.StartInfo.FileName = _ffmpegYolu; // Çalışacak program (ffmpeg.exe)
                islem.StartInfo.Arguments = komutlar; // Programa vereceğimiz komutlar
                islem.StartInfo.UseShellExecute = false; // Kendi penceremizde yakalamak için false yapıyoruz
                islem.StartInfo.CreateNoWindow = true; // Siyah komut penceresini (CMD) ekranda gösterme (gizli çalışsın)
                islem.StartInfo.RedirectStandardError = true; // FFmpeg mesajlarını ekrandan yakalamak için (FFmpeg mesajları genelde Error kanalından gelir)

                TimeSpan toplamSure = TimeSpan.Zero;

                // Program her mesaj ürettiğinde bu blok çalışır.
                islem.ErrorDataReceived += (gonderen, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    // Mesajı arayüze gönderiyoruz. (Soru işareti (?) null değilse gönder demek)
                    BilgiMesajiGeldiginde?.Invoke(e.Data);

                    // Videonun toplam süresini bulmaya çalışıyoruz. (İlerleme çubuğu için lazım)
                    if (toplamSure == TimeSpan.Zero && e.Data.Contains("Duration:"))
                    {
                        var eslesme = Regex.Match(e.Data, @"Duration:\s(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                        if (eslesme.Success)
                        {
                            toplamSure = new TimeSpan(
                                0,
                                int.Parse(eslesme.Groups[1].Value),
                                int.Parse(eslesme.Groups[2].Value),
                                int.Parse(eslesme.Groups[3].Value),
                                int.Parse(eslesme.Groups[4].Value) * 10);
                        }
                    }

                    // Şu an videonun neresinde (hangi saniyesinde) olduğumuzu buluyoruz.
                    var zamanEslesmesi = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                    if (zamanEslesmesi.Success && toplamSure.TotalSeconds > 0)
                    {
                        TimeSpan suAnkiZaman = new TimeSpan(
                            0,
                            int.Parse(zamanEslesmesi.Groups[1].Value),
                            int.Parse(zamanEslesmesi.Groups[2].Value),
                            int.Parse(zamanEslesmesi.Groups[3].Value),
                            int.Parse(zamanEslesmesi.Groups[4].Value) * 10);

                        // Matematik: (Şu anki saniye / Toplam Saniye) * 100 = Yüzdelik durum
                        double yuzde = (suAnkiZaman.TotalSeconds / toplamSure.TotalSeconds) * 100;
                        if (yuzde > 100) yuzde = 100;

                        // Yüzdeyi ve süreyi ekrana (arayüze) gönder.
                        İlerlemeDurumuDegistiginde?.Invoke(yuzde, suAnkiZaman.ToString(@"hh\:mm\:ss"));
                    }
                };

                try
                {
                    islem.Start(); // Programı başlat
                    islem.BeginErrorReadLine(); // Mesajları okumaya başla
                    await islem.WaitForExitAsync(); // Programın işini bitirmesini bekle

                    // Eğer işlem kodu 0 değilse bir şeyler ters gitmiştir.
                    if (islem.ExitCode != 0)
                    {
                        throw new FfmpegException($"Sıkıştırma sırasında hata oluştu. Hata Kodu: {islem.ExitCode}");
                    }
                }
                catch (Exception hata)
                {
                    throw new FfmpegException("Video işleme başlatılamadı. FFmpeg dosyasının eksik olmadığından emin olun.", hata);
                }
            }
        }
    }
}
