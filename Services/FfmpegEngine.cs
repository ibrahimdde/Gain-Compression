using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using FfmpegWrapper.Exceptions;
using FfmpegWrapper.Models;

namespace FfmpegWrapper.Services
{
    public class FfmpegEngine
    {
        private readonly string _ffmpegPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "ffmpeg", "ffmpeg.exe");

        // UI'a bilgi göndermek için Event (Action delegate kullanıyoruz)
        public event Action<double, string> OnProgressChanged;
        public event Action<string> OnLogReceived;

        public FfmpegEngine()
        {
            // FFMPEG yolu program ilk açıldığında henüz indirilmemiş olabilir.
            // O yüzden burada _ffmpegPath'i değiştirmemeliyiz, FfmpegDownloader onu doğru yere kuracak.
        }

        public async Task<string> CompressVideoAsync(VideoFile inputVideo, string outputDirectory, CompressionProfile profile)
        {
            string outputFileName = $"{inputVideo.FileName}_compressed{inputVideo.FileExtension}";
            string outputPath = Path.Combine(outputDirectory, outputFileName);

            int counter = 1;
            while (File.Exists(outputPath))
            {
                outputFileName = $"{inputVideo.FileName}_compressed({counter}){inputVideo.FileExtension}";
                outputPath = Path.Combine(outputDirectory, outputFileName);
                counter++;
            }

            // -y varsa üstüne yazar, -i input dosyası
            string arguments = $"-y -i \"{inputVideo.FilePath}\" ";
            
            if (!string.IsNullOrEmpty(profile.Resolution) && profile.Resolution.Contains("x"))
            {
                arguments += $"-vf scale={profile.Resolution.Replace("x", ":")} ";
            }

            arguments += $"-b:v {profile.Bitrate}k -r {profile.Fps} \"{outputPath}\"";

            await RunFfmpegProcessAsync(arguments);
            return outputPath;
        }

        private async Task RunFfmpegProcessAsync(string arguments)
        {
            using (Process process = new Process())
            {
                process.StartInfo.FileName = _ffmpegPath;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.CreateNoWindow = true;
                process.StartInfo.RedirectStandardError = true; // FFmpeg çıktıları genelde error stream'den gelir.

                TimeSpan totalDuration = TimeSpan.Zero;

                process.ErrorDataReceived += (sender, e) =>
                {
                    if (string.IsNullOrEmpty(e.Data)) return;

                    OnLogReceived?.Invoke(e.Data);

                    // Toplam süreyi bul (Duration: 00:01:23.45)
                    if (totalDuration == TimeSpan.Zero && e.Data.Contains("Duration:"))
                    {
                        var match = Regex.Match(e.Data, @"Duration:\s(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                        if (match.Success)
                        {
                            totalDuration = new TimeSpan(
                                0,
                                int.Parse(match.Groups[1].Value),
                                int.Parse(match.Groups[2].Value),
                                int.Parse(match.Groups[3].Value),
                                int.Parse(match.Groups[4].Value) * 10);
                        }
                    }

                    // İşlenen süreyi bul (time=00:00:15.32)
                    var timeMatch = Regex.Match(e.Data, @"time=(\d{2}):(\d{2}):(\d{2})\.(\d{2})");
                    if (timeMatch.Success && totalDuration.TotalSeconds > 0)
                    {
                        TimeSpan currentTime = new TimeSpan(
                            0,
                            int.Parse(timeMatch.Groups[1].Value),
                            int.Parse(timeMatch.Groups[2].Value),
                            int.Parse(timeMatch.Groups[3].Value),
                            int.Parse(timeMatch.Groups[4].Value) * 10);

                        double percentage = (currentTime.TotalSeconds / totalDuration.TotalSeconds) * 100;
                        if (percentage > 100) percentage = 100;

                        OnProgressChanged?.Invoke(percentage, currentTime.ToString(@"hh\:mm\:ss"));
                    }
                };

                try
                {
                    process.Start();
                    process.BeginErrorReadLine();
                    await process.WaitForExitAsync();

                    if (process.ExitCode != 0)
                    {
                        throw new FfmpegException($"FFmpeg işlemi hata ile sonlandı. Kod: {process.ExitCode}");
                    }
                }
                catch (Exception ex)
                {
                    throw new FfmpegException("FFmpeg çalıştırılırken bir hata oluştu. FFMPEG'in 'ffmpeg' klasöründe veya uygulama dizininde olduğundan emin olun.", ex);
                }
            }
        }
    }
}
