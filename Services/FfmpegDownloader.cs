using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FfmpegWrapper.Services
{
    // Bu sınıf, programın çalışması için gereken "FFmpeg" uygulamasını internetten indirmek için var.
    public class FfmpegDownloader
    {
        // FFmpeg'in indirileceği güvenli adres.
        private const string IndirmeAdresi = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        
        // Ekrana bilgi göndermek için olaylar (Event) tanımlıyoruz.
        public event Action<double> IndirmeYuzdesiDegistiginde;
        public event Action<string> DurumMesajiGeldiginde;

        // FFmpeg eksikse indirme işlemini başlatan metot.
        public async Task FfmpegYoksaIndirAsync()
        {
            // Programımızın çalıştığı klasörü buluyoruz.
            string anaKlasor = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegKlasoru = Path.Combine(anaKlasor, "ffmpeg");
            string ffmpegProgrami = Path.Combine(ffmpegKlasoru, "ffmpeg.exe");

            // Eğer "ffmpeg.exe" zaten bilgisayarda varsa, hiçbir şey yapmadan metottan çık (return).
            if (File.Exists(ffmpegProgrami))
            {
                return; 
            }

            // Arayüze mesaj gönder:
            DurumMesajiGeldiginde?.Invoke("Gerekli araçlar (FFmpeg) bulunamadı. Sizin için otomatik indiriliyor, lütfen bekleyin...");

            // Klasör yoksa oluştur.
            if (!Directory.Exists(ffmpegKlasoru))
            {
                Directory.CreateDirectory(ffmpegKlasoru);
            }

            // İndireceğimiz arşiv (zip) dosyası ve onu çıkartacağımız geçici klasör.
            string zipDosyasi = Path.Combine(anaKlasor, "ffmpeg_temp.zip");
            string cikartmaKlasoru = Path.Combine(anaKlasor, "ffmpeg_extracted");

            try
            {
                // İnternetten dosya indirmek için HttpClient kullanıyoruz.
                using (var internetBaglantisi = new HttpClient())
                {
                    // Bazı siteler robot olduğumuzu sanmasın diye tarayıcı kimliği gönderiyoruz.
                    internetBaglantisi.DefaultRequestHeaders.Add("User-Agent", "OgrenciUygulamasi/1.0");

                    // İndirme isteği gönderiyoruz.
                    using (var cevap = await internetBaglantisi.GetAsync(IndirmeAdresi, HttpCompletionOption.ResponseHeadersRead))
                    {
                        cevap.EnsureSuccessStatusCode(); // Hata varsa (Örn: 404 Not Found) programı durdurur ve hataya düşer.

                        var toplamBoyut = cevap.Content.Headers.ContentLength ?? -1L; // Dosyanın toplam boyutu
                        var yuzdeHesaplanabilirMi = toplamBoyut != -1;

                        // Dosyayı yavaş yavaş okuyup (stream), kendi bilgisayarımıza yazıyoruz.
                        using (var gelenVeriAkisi = await cevap.Content.ReadAsStreamAsync())
                        using (var dosyaYazici = new FileStream(zipDosyasi, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var paket = new byte[8192];
                            long okunanToplam = 0;
                            int okunanPaketBoyutu;

                            // Veri akışı bitene kadar döngüye devam et (İndirme işlemi)
                            while ((okunanPaketBoyutu = await gelenVeriAkisi.ReadAsync(paket, 0, paket.Length)) != 0)
                            {
                                await dosyaYazici.WriteAsync(paket, 0, okunanPaketBoyutu);
                                okunanToplam += okunanPaketBoyutu;

                                // İndirme yüzdesini hesaplayıp ekrana gönderiyoruz.
                                if (yuzdeHesaplanabilirMi)
                                {
                                    double yuzde = (double)okunanToplam / toplamBoyut * 100;
                                    IndirmeYuzdesiDegistiginde?.Invoke(yuzde);
                                }
                            }
                        }
                    }
                }

                DurumMesajiGeldiginde?.Invoke("İndirme tamamlandı. Dosyalar arşivden (zip) çıkarılıyor...");

                // Eski bir klasör varsa temizle.
                if (Directory.Exists(cikartmaKlasoru))
                    Directory.Delete(cikartmaKlasoru, true);

                // Zip dosyasını klasöre çıkart.
                ZipFile.ExtractToDirectory(zipDosyasi, cikartmaKlasoru);

                // Çıkarılan klasörlerin içinde "ffmpeg.exe" dosyasının tam yerini bul.
                var bulunanProgram = Directory.GetFiles(cikartmaKlasoru, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                
                if (bulunanProgram != null)
                {
                    File.Copy(bulunanProgram, ffmpegProgrami, true); // Dosyayı asıl olması gereken yere kopyala.
                }
                else
                {
                    throw new FileNotFoundException("İndirilen dosyanın içinden ffmpeg.exe çıkmadı!");
                }

                DurumMesajiGeldiginde?.Invoke("Kurulum Başarılı! Uygulama kullanılmaya hazır.");
            }
            finally
            {
                // İşlem başarılı da olsa, hata da verse çöpleri (geçici dosyaları) temizliyoruz.
                // try-catch koyuyoruz ki dosyalar kullanımda vs. olursa hata fırlatmasın.
                DurumMesajiGeldiginde?.Invoke("Geçici dosyalar temizleniyor...");
                try { if (File.Exists(zipDosyasi)) File.Delete(zipDosyasi); } catch { }
                try { if (Directory.Exists(cikartmaKlasoru)) Directory.Delete(cikartmaKlasoru, true); } catch { }
            }
        }
    }
}
