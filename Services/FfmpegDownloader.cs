using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace FfmpegWrapper.Services
{
    public class FfmpegDownloader
    {
        // Güvenilir GitHub kaynağından FFMPEG Windows derlemesi
        private const string FfmpegUrl = "https://github.com/BtbN/FFmpeg-Builds/releases/download/latest/ffmpeg-master-latest-win64-gpl.zip";
        
        public event Action<double> OnDownloadProgressChanged;
        public event Action<string> OnDownloadStatusChanged;

        public async Task DownloadFfmpegIfNeededAsync()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string ffmpegDir = Path.Combine(baseDir, "ffmpeg");
            string ffmpegExe = Path.Combine(ffmpegDir, "ffmpeg.exe");

            // Eğer ffmpeg.exe mevcutsa indirmeye gerek yok
            if (File.Exists(ffmpegExe))
            {
                return; 
            }

            OnDownloadStatusChanged?.Invoke("Sistemde FFMPEG bulunamadı. İlk kurulum için indiriliyor (Lütfen bekleyin)...");

            if (!Directory.Exists(ffmpegDir))
            {
                Directory.CreateDirectory(ffmpegDir);
            }

            string zipPath = Path.Combine(baseDir, "ffmpeg_temp.zip");
            string extractPath = Path.Combine(baseDir, "ffmpeg_extracted");

            try
            {
                using (var client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("User-Agent", "FfmpegWrapper/1.0");

                    using (var response = await client.GetAsync(FfmpegUrl, HttpCompletionOption.ResponseHeadersRead))
                    {
                        response.EnsureSuccessStatusCode();

                        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
                        var canReportProgress = totalBytes != -1;

                        using (var contentStream = await response.Content.ReadAsStreamAsync())
                        using (var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true))
                        {
                            var buffer = new byte[8192];
                            long totalRead = 0;
                            int bytesRead;

                            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await fileStream.WriteAsync(buffer, 0, bytesRead);
                                totalRead += bytesRead;

                                if (canReportProgress)
                                {
                                    double progress = (double)totalRead / totalBytes * 100;
                                    OnDownloadProgressChanged?.Invoke(progress);
                                }
                            }
                        }
                    }
                }

                OnDownloadStatusChanged?.Invoke("İndirme tamamlandı. Dosyalar arşivden çıkarılıyor...");

                // Çıkarma işlemi için klasör hazırlığı
                if (Directory.Exists(extractPath))
                    Directory.Delete(extractPath, true);

                ZipFile.ExtractToDirectory(zipPath, extractPath);

                // Çıkarılan klasörlerin içinde ffmpeg.exe dosyasını bul (alt klasörlerde olabilir)
                var extractedExePath = Directory.GetFiles(extractPath, "ffmpeg.exe", SearchOption.AllDirectories).FirstOrDefault();
                
                if (extractedExePath != null)
                {
                    File.Copy(extractedExePath, ffmpegExe, true);
                }
                else
                {
                    throw new FileNotFoundException("İndirilen arşivin içerisinde ffmpeg.exe bulunamadı!");
                }

                OnDownloadStatusChanged?.Invoke("Kurulum Başarılı! Uygulama kullanıma hazır.");
            }
            finally
            {
                // İşlem başarılı da olsa hata da verse çöp (geçici) dosyaları temizle
                OnDownloadStatusChanged?.Invoke("Geçici dosyalar temizleniyor...");
                try { if (File.Exists(zipPath)) File.Delete(zipPath); } catch { }
                try { if (Directory.Exists(extractPath)) Directory.Delete(extractPath, true); } catch { }
            }
        }
    }
}
