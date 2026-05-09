using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using System.Diagnostics;
using Microsoft.Win32;
using FfmpegWrapper.Models;
using FfmpegWrapper.Services;

namespace FfmpegWrapper
{
    public partial class MainWindow : Window
    {
        private readonly ProfileManager _profileManager;
        private readonly FfmpegEngine _ffmpegEngine;
        private VideoFile _currentVideo;
        private string _outputDirectory;
        private Stopwatch _stopwatch;
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            _profileManager = new ProfileManager();
            _ffmpegEngine = new FfmpegEngine();

            _ffmpegEngine.OnProgressChanged += FfmpegEngine_OnProgressChanged;
            _ffmpegEngine.OnLogReceived += FfmpegEngine_OnLogReceived;

            _stopwatch = new Stopwatch();
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _timer.Tick += (s, e) => txtTime.Text = $"Geçen Süre: {_stopwatch.Elapsed:hh\\:mm\\:ss}";

            LoadProfilesToUI();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var downloader = new FfmpegDownloader();
            
            downloader.OnDownloadProgressChanged += (percentage) =>
            {
                Dispatcher.Invoke(() =>
                {
                    progressBar.Value = percentage;
                    txtPercentage.Text = $"%{percentage:F1}";
                });
            };

            downloader.OnDownloadStatusChanged += (status) =>
            {
                Dispatcher.Invoke(() => LogToUI(status));
            };

            btnStart.IsEnabled = false; // İndirme bitene kadar her şeyi kitle
            grpFileOps.IsEnabled = false;
            grpProfileOps.IsEnabled = false;

            try
            {
                await downloader.DownloadFfmpegIfNeededAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show("FFMPEG indirilirken bir hata oluştu. Lütfen internet bağlantınızı kontrol edin.\n\nDetay: " + ex.Message, "İndirme Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btnStart.IsEnabled = true; // Tüm tuşları tekrar aktif et
                grpFileOps.IsEnabled = true;
                grpProfileOps.IsEnabled = true;
                progressBar.Value = 0;
                txtPercentage.Text = "%0.0";
            }
        }

        private void LoadProfilesToUI()
        {
            cmbProfiles.ItemsSource = null;
            cmbProfiles.ItemsSource = _profileManager.GetAllProfiles();
            if (cmbProfiles.Items.Count > 0)
                cmbProfiles.SelectedIndex = 0;
        }

        private void CmbProfiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is CompressionProfile profile)
            {
                txtProfileName.Text = profile.ProfileName;
                txtResolution.Text = profile.Resolution;
                txtBitrate.Text = profile.Bitrate.ToString();
                txtFps.Text = profile.Fps.ToString();
            }
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            cmbProfiles.SelectedIndex = -1;
            txtProfileName.Text = "Yeni Profil";
            txtResolution.Text = "1920x1080";
            txtBitrate.Text = "2000";
            txtFps.Text = "30";
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            if (!int.TryParse(txtBitrate.Text, out int bitrate) || !int.TryParse(txtFps.Text, out int fps))
            {
                MessageBox.Show("Bitrate ve FPS sadece sayı olmalıdır.", "Hata", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (cmbProfiles.SelectedItem is CompressionProfile selectedProfile)
            {
                // Güncelle (Update)
                selectedProfile.ProfileName = txtProfileName.Text;
                selectedProfile.Resolution = txtResolution.Text;
                selectedProfile.Bitrate = bitrate;
                selectedProfile.Fps = fps;
                _profileManager.UpdateProfile(selectedProfile);
                MessageBox.Show("Profil başarıyla güncellendi.");
            }
            else
            {
                // Yeni Ekle (Create)
                var newProfile = new CompressionProfile
                {
                    ProfileName = txtProfileName.Text,
                    Resolution = txtResolution.Text,
                    Bitrate = bitrate,
                    Fps = fps
                };
                _profileManager.AddProfile(newProfile);
                MessageBox.Show("Yeni profil başarıyla eklendi.");
            }
            LoadProfilesToUI();
            
            // Yeni eklenen profili seç
            cmbProfiles.SelectedItem = _profileManager.GetAllProfiles().LastOrDefault(p => p.ProfileName == txtProfileName.Text);
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            if (cmbProfiles.SelectedItem is CompressionProfile selectedProfile)
            {
                var result = MessageBox.Show($"{selectedProfile.ProfileName} silinecek, emin misiniz?", "Onay", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    _profileManager.DeleteProfile(selectedProfile.Id);
                    LoadProfilesToUI();
                }
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Video Dosyaları|*.mp4;*.mkv;*.avi;*.mov",
                Title = "Sıkıştırılacak Videoyu Seçin"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    _currentVideo = new VideoFile(openFileDialog.FileName);
                    txtInputFile.Text = _currentVideo.FilePath;
                    
                    // Otomatik olarak dosyanın bulunduğu klasörü çıktı olarak belirle
                    _outputDirectory = System.IO.Path.GetDirectoryName(_currentVideo.FilePath);
                    txtOutputDir.Text = _outputDirectory;
                    
                    LogToUI("Dosya Seçildi: " + _currentVideo.GetMediaDescription());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Dosya Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnSelectOutputDir_Click(object sender, RoutedEventArgs e)
        {
            // .NET 8 WPF ile gelen OpenFolderDialog
            OpenFolderDialog openFolderDialog = new OpenFolderDialog
            {
                Title = "Çıktı Klasörünü Seçin"
            };

            if (openFolderDialog.ShowDialog() == true)
            {
                _outputDirectory = openFolderDialog.FolderName;
                txtOutputDir.Text = _outputDirectory;
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_currentVideo == null)
            {
                MessageBox.Show("Lütfen önce bir video seçin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!int.TryParse(txtBitrate.Text, out int bitrate) || !int.TryParse(txtFps.Text, out int fps))
            {
                MessageBox.Show("Lütfen geçerli değerler girin.", "Uyarı", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var activeProfile = new CompressionProfile
            {
                Resolution = txtResolution.Text,
                Bitrate = bitrate,
                Fps = fps
            };

            btnStart.IsEnabled = false;
            progressBar.Value = 0;
            txtPercentage.Text = "%0.0";
            txtTime.Text = "Geçen Süre: 00:00:00";
            _stopwatch.Restart();
            _timer.Start();
            LogToUI("--- SIKIŞTIRMA İŞLEMİ BAŞLADI ---");
            LogToUI($"Hedef Çözünürlük: {activeProfile.Resolution}, Bitrate: {activeProfile.Bitrate}k, FPS: {activeProfile.Fps}");

            try
            {
                string finalPath = await _ffmpegEngine.CompressVideoAsync(_currentVideo, _outputDirectory, activeProfile);
                
                // İşlem biter bitmez süreyi durdur (Kullanıcının mesaja tıklamasını beklemeden)
                _stopwatch.Stop();
                _timer.Stop();
                
                LogToUI("--- İŞLEM BAŞARIYLA TAMAMLANDI ---");
                MessageBox.Show("Sıkıştırma işlemi başarıyla tamamlandı!\n\nKaydedilen Dosya:\n" + finalPath, "Başarılı", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                _stopwatch.Stop();
                _timer.Stop();
                
                LogToUI($"HATA: {ex.Message}");
                MessageBox.Show(ex.Message, "İşlem Hatası", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _stopwatch.Stop();
                _timer.Stop();
                btnStart.IsEnabled = true;
            }
        }

        private void FfmpegEngine_OnProgressChanged(double percentage, string currentTime)
        {
            Dispatcher.Invoke(() =>
            {
                progressBar.Value = percentage;
                txtPercentage.Text = $"%{percentage:F1}";
            });
        }

        private void FfmpegEngine_OnLogReceived(string log)
        {
            Dispatcher.Invoke(() =>
            {
                // Frame ile başlayan spam logları konsolu şişirmesin diye filtreleyebiliriz.
                // İsteğe bağlı olarak sadece önemli hataları veya duration loglarını bırakabilirsiniz.
                if (!log.StartsWith("frame="))
                {
                    LogToUI(log);
                }
            });
        }

        private void LogToUI(string message)
        {
            txtLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {message}\n");
            txtLog.ScrollToEnd();
        }
    }
}