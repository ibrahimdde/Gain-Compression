using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using FfmpegWrapper.Models;
using FfmpegWrapper.Services;

namespace FfmpegWrapper
{
    public partial class MainWindow : Window
    {
        // Programımızda kullanacağımız "Motorlar" (Servisler)
        private ProfileManager _profilYoneticisi;
        private FfmpegEngine _videoMotoru;
        
        // Seçilen videoyu ve klasörü hafızada tutmak için değişkenler
        private VideoFile _secilenVideo;
        private string _secilenKlasor;
        
        // İşlem süresini hesaplamak için saati tuttuğumuz değişken
        private DateTime _islemBaslangicZamani;

        public MainWindow()
        {
            InitializeComponent();
            
            // Motorları çalışmaya hazır hale getiriyoruz (Yeni birer kopyasını oluşturuyoruz)
            _profilYoneticisi = new ProfileManager();
            _videoMotoru = new FfmpegEngine();

            // Video motoru arka planda çalışırken bize haber verebilmesi için olayları (Event) dinliyoruz
            _videoMotoru.İlerlemeDurumuDegistiginde += Motor_IlerlemeDurumuDegistiginde;
            _videoMotoru.BilgiMesajiGeldiginde += Motor_BilgiMesajiGeldiginde;

            // Ekran açılır açılmaz profilleri ComboBox'a (açılır listeye) dolduruyoruz
            ProfilleriEkranaYukle();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Program ilk açıldığında, internetten FFmpeg indirme motorunu hazırlıyoruz
            FfmpegDownloader indirici = new FfmpegDownloader();
            
            indirici.IndirmeYuzdesiDegistiginde += Indirici_IndirmeYuzdesiDegistiginde;
            indirici.DurumMesajiGeldiginde += Indirici_DurumMesajiGeldiginde;

            // İndirme işlemi bitene kadar butonları kilitliyoruz ki kullanıcı acele edip hata almasın
            btnStart.IsEnabled = false;
            grpFileOps.IsEnabled = false;
            grpProfileOps.IsEnabled = false;

            try
            {
                // İndirme işlemini başlat (Eğer zaten varsa hemen bitecek)
                await indirici.FfmpegYoksaIndirAsync();
            }
            catch (Exception hata)
            {
                MessageBox.Show("İndirme sırasında bir hata oluştu: " + hata.Message);
            }

            // İndirme bittikten sonra butonların kilidini aç
            btnStart.IsEnabled = true;
            grpFileOps.IsEnabled = true;
            grpProfileOps.IsEnabled = true;
        }

        // --- İNDİRİCİ OLAYLARI ---
        private void Indirici_IndirmeYuzdesiDegistiginde(double yuzde)
        {
            // Arka plan işlemlerinden (indirici) ekrandaki çubuğu güncellemek için Dispatcher kullanmak zorundayız.
            // Dispatcher, "Arka plandaki işçi, ekrandaki çubuğa dokunamaz, bu yüzden ana ekrana rica eder" mantığıdır.
            Dispatcher.Invoke(new Action(delegate() 
            {
                progressBar.Value = yuzde;
                txtPercentage.Text = "%" + yuzde.ToString("F1");
            }));
        }

        private void Indirici_DurumMesajiGeldiginde(string mesaj)
        {
            Dispatcher.Invoke(new Action(delegate() 
            {
                EkranaMesajYaz(mesaj);
            }));
        }

        // --- VİDEO MOTORU OLAYLARI ---
        private void Motor_IlerlemeDurumuDegistiginde(double yuzde, string gecenSure)
        {
            Dispatcher.Invoke(new Action(delegate() 
            {
                progressBar.Value = yuzde;
                txtPercentage.Text = "%" + yuzde.ToString("F1");
                
                // Gerçek geçen süreyi hesaplıyoruz (Şu anki saatten - işlemin başladığı saati çıkar)
                TimeSpan gercekGecenSure = DateTime.Now - _islemBaslangicZamani;
                txtTime.Text = "Geçen Süre: " + gercekGecenSure.ToString(@"hh\:mm\:ss");
            }));
        }

        private void Motor_BilgiMesajiGeldiginde(string mesaj)
        {
            Dispatcher.Invoke(new Action(delegate() 
            {
                if (!mesaj.StartsWith("frame=")) // Ekrana çok fazla yazı dolmasın diye gereksizleri süzüyoruz
                {
                    EkranaMesajYaz(mesaj);
                }
            }));
        }

        // --- EKRAN YARDIMCI METOTLARI ---
        private void ProfilleriEkranaYukle()
        {
            // Listeyi temizleyip yeniden dolduruyoruz
            cmbProfiles.ItemsSource = null;
            cmbProfiles.ItemsSource = _profilYoneticisi.GetAllProfiles();
            
            // Eğer listede en az bir profil varsa, ilkini otomatik seçili yap
            if (cmbProfiles.Items.Count > 0)
            {
                cmbProfiles.SelectedIndex = 0;
            }
        }

        private void EkranaMesajYaz(string mesaj)
        {
            // Mesajın başına saati ekleyip kutuya yazdırıyoruz
            txtLog.AppendText("[" + DateTime.Now.ToShortTimeString() + "] " + mesaj + "\n");
            txtLog.ScrollToEnd(); // Otomatik olarak en aşağı kaydır
        }

        // --- BUTON TIKLAMA OLAYLARI ---

        // Listeden farklı bir profil seçildiğinde burası çalışır
        private void CmbProfiles_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Seçilen öğeyi CompressionProfile olarak al
            CompressionProfile secilenProfil = (CompressionProfile)cmbProfiles.SelectedItem;
            
            if (secilenProfil != null)
            {
                txtProfileName.Text = secilenProfil.ProfilAdi;
                txtResolution.Text = secilenProfil.Cozunurluk;
                txtBitrate.Text = secilenProfil.Bitrate.ToString();
                txtFps.Text = secilenProfil.Fps.ToString();

                // Kodek bilgisini kutuda seçili hale getir
                if (secilenProfil.VideoKodek == "libx265") cmbCodec.SelectedIndex = 1;
                else if (secilenProfil.VideoKodek == "libaom-av1") cmbCodec.SelectedIndex = 2;
                else cmbCodec.SelectedIndex = 0; // Varsayılan H264

                // Hız (Preset) bilgisini kutuda seçili hale getir
                if (secilenProfil.HizOnayari == "ultrafast") cmbPreset.SelectedIndex = 0;
                else if (secilenProfil.HizOnayari == "fast") cmbPreset.SelectedIndex = 1;
                else if (secilenProfil.HizOnayari == "slow") cmbPreset.SelectedIndex = 3;
                else cmbPreset.SelectedIndex = 2; // Varsayılan medium
            }
        }

        private void BtnNewProfile_Click(object sender, RoutedEventArgs e)
        {
            // Seçimi iptal et ve kutuları varsayılan değerlerle doldur
            cmbProfiles.SelectedIndex = -1;
            txtProfileName.Text = "Yeni Profil";
            txtResolution.Text = "1920x1080";
            txtBitrate.Text = "2000";
            txtFps.Text = "30";
            cmbCodec.SelectedIndex = 0;
            cmbPreset.SelectedIndex = 2;
        }

        private void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            // Kullanıcının girdiği yazıları sayıya çevirmeye çalışıyoruz
            int bitrate = 0;
            int fps = 0;
            bool bitrateDogruMu = int.TryParse(txtBitrate.Text, out bitrate);
            bool fpsDogruMu = int.TryParse(txtFps.Text, out fps);

            if (bitrateDogruMu == false || fpsDogruMu == false)
            {
                MessageBox.Show("Bitrate ve FPS alanlarına sadece sayı girmelisiniz!");
                return; // Hatayı göster ve metodu burada durdur
            }

            // Seçili Kodek (Video Formatı) bilgisini al
            System.Windows.Controls.ComboBoxItem secilenCodecKutusu = (System.Windows.Controls.ComboBoxItem)cmbCodec.SelectedItem;
            string codecYazisi = secilenCodecKutusu.Content.ToString();
            string kodekDegeri = "libx264"; // Varsayılan
            if (codecYazisi.Contains("H265")) kodekDegeri = "libx265";
            if (codecYazisi.Contains("AV1")) kodekDegeri = "libaom-av1";

            // Seçili Hız (Preset) bilgisini al
            System.Windows.Controls.ComboBoxItem secilenHizKutusu = (System.Windows.Controls.ComboBoxItem)cmbPreset.SelectedItem;
            string hizYazisi = secilenHizKutusu.Content.ToString();
            string hizDegeri = "medium";
            if (hizYazisi.Contains("Çok Hızlı")) hizDegeri = "ultrafast";
            else if (hizYazisi.Contains("Hızlı")) hizDegeri = "fast";
            else if (hizYazisi.Contains("Yavaş")) hizDegeri = "slow";

            CompressionProfile secilenProfil = (CompressionProfile)cmbProfiles.SelectedItem;

            if (secilenProfil != null)
            {
                // Eğer listeden biri seçiliyse, onu güncelle
                secilenProfil.ProfilAdi = txtProfileName.Text;
                secilenProfil.Cozunurluk = txtResolution.Text;
                secilenProfil.Bitrate = bitrate;
                secilenProfil.Fps = fps;
                secilenProfil.VideoKodek = kodekDegeri;
                secilenProfil.HizOnayari = hizDegeri;
                
                _profilYoneticisi.UpdateProfile(secilenProfil);
                MessageBox.Show("Profil güncellendi.");
            }
            else
            {
                // Eğer listeden biri seçili değilse, yeni bir tane oluştur
                CompressionProfile yeniProfil = new CompressionProfile();
                yeniProfil.ProfilAdi = txtProfileName.Text;
                yeniProfil.Cozunurluk = txtResolution.Text;
                yeniProfil.Bitrate = bitrate;
                yeniProfil.Fps = fps;
                yeniProfil.VideoKodek = kodekDegeri;
                yeniProfil.HizOnayari = hizDegeri;

                _profilYoneticisi.AddProfile(yeniProfil);
                MessageBox.Show("Yeni profil kaydedildi.");
            }

            // Listeyi yenile
            ProfilleriEkranaYukle();
            
            // Yeni eklenen profili listede seçili hale getirmek için
            List<CompressionProfile> tumProfiller = _profilYoneticisi.GetAllProfiles();
            for (int i = 0; i < tumProfiller.Count; i++)
            {
                if (tumProfiller[i].ProfilAdi == txtProfileName.Text)
                {
                    cmbProfiles.SelectedItem = tumProfiller[i];
                }
            }
        }

        private void BtnDeleteProfile_Click(object sender, RoutedEventArgs e)
        {
            CompressionProfile secilenProfil = (CompressionProfile)cmbProfiles.SelectedItem;

            if (secilenProfil != null)
            {
                // Silmeden önce emin misin diye sor
                MessageBoxResult cevap = MessageBox.Show(secilenProfil.ProfilAdi + " silinecek, emin misiniz?", "Soru", MessageBoxButton.YesNo);
                
                if (cevap == MessageBoxResult.Yes)
                {
                    _profilYoneticisi.DeleteProfile(secilenProfil.IdyiGetir());
                    ProfilleriEkranaYukle();
                }
            }
        }

        private void VideoyuAyarla(string dosyaYolu)
        {
            try
            {
                // Kullanıcının seçtiği veya sürüklediği dosyayı kullanarak VideoFile sınıfından bir örnek oluşturuyoruz
                _secilenVideo = new VideoFile(dosyaYolu);
                txtInputFile.Text = _secilenVideo.DosyaYolu;
                
                // Seçilen videonun bulunduğu klasörü, otomatik olarak çıktı klasörü yapıyoruz
                _secilenKlasor = System.IO.Path.GetDirectoryName(_secilenVideo.DosyaYolu);
                txtOutputDir.Text = _secilenKlasor;
                
                EkranaMesajYaz("Dosya Seçildi: " + _secilenVideo.AciklamaGetir());
            }
            catch (Exception hata)
            {
                MessageBox.Show("Dosya açılırken hata oluştu: " + hata.Message);
            }
        }

        private void Pencere_DosyaBirakildiginda(object sender, DragEventArgs e)
        {
            // Sürüklenen şey bir dosya mı kontrol ediyoruz
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                // Bilgisayardan sürüklenen dosyaların yolunu liste olarak alıyoruz
                string[] dosyalar = (string[])e.Data.GetData(DataFormats.FileDrop);
                
                if (dosyalar.Length > 0)
                {
                    // İlk sürüklenen dosyayı al ve programa tanıt
                    VideoyuAyarla(dosyalar[0]);
                }
            }
        }

        private void BtnSelectFile_Click(object sender, RoutedEventArgs e)
        {
            // Windows'un standart dosya seçme penceresi
            OpenFileDialog dosyaPenceresi = new OpenFileDialog();
            dosyaPenceresi.Filter = "Video Dosyaları|*.mp4;*.mkv;*.avi;*.mov";
            dosyaPenceresi.Title = "Sıkıştırılacak Videoyu Seçin";

            bool? sonuc = dosyaPenceresi.ShowDialog();

            if (sonuc == true)
            {
                VideoyuAyarla(dosyaPenceresi.FileName);
            }
        }

        private void BtnSelectOutputDir_Click(object sender, RoutedEventArgs e)
        {
            // Çıktı için klasör seçme penceresi
            OpenFolderDialog klasorPenceresi = new OpenFolderDialog();
            klasorPenceresi.Title = "Nereye Kaydedilecek?";

            if (klasorPenceresi.ShowDialog() == true)
            {
                _secilenKlasor = klasorPenceresi.FolderName;
                txtOutputDir.Text = _secilenKlasor;
            }
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (_secilenVideo == null)
            {
                MessageBox.Show("Lütfen önce bir video seçin!");
                return;
            }

            int bitrate = 0;
            int fps = 0;
            if (!int.TryParse(txtBitrate.Text, out bitrate) || !int.TryParse(txtFps.Text, out fps))
            {
                MessageBox.Show("FPS veya Bitrate sayı olmalıdır!");
                return;
            }

            // O anki ekrandaki ayarları alıp geçici bir profil yapıyoruz
            CompressionProfile seciliAyarlar = new CompressionProfile();
            seciliAyarlar.Cozunurluk = txtResolution.Text;
            seciliAyarlar.Bitrate = bitrate;
            seciliAyarlar.Fps = fps;

            // Seçili Kodek bilgisini al
            System.Windows.Controls.ComboBoxItem secilenCodecKutusu = (System.Windows.Controls.ComboBoxItem)cmbCodec.SelectedItem;
            string codecYazisi = secilenCodecKutusu.Content.ToString();
            string kodekDegeri = "libx264";
            if (codecYazisi.Contains("H265")) kodekDegeri = "libx265";
            if (codecYazisi.Contains("AV1")) kodekDegeri = "libaom-av1";

            // Seçili Hız bilgisini al
            System.Windows.Controls.ComboBoxItem secilenHizKutusu = (System.Windows.Controls.ComboBoxItem)cmbPreset.SelectedItem;
            string hizYazisi = secilenHizKutusu.Content.ToString();
            string hizDegeri = "medium";
            if (hizYazisi.Contains("Çok Hızlı")) hizDegeri = "ultrafast";
            else if (hizYazisi.Contains("Hızlı")) hizDegeri = "fast";
            else if (hizYazisi.Contains("Yavaş")) hizDegeri = "slow";

            seciliAyarlar.VideoKodek = kodekDegeri;
            seciliAyarlar.HizOnayari = hizDegeri;

            btnStart.IsEnabled = false; // İşlem bitene kadar tekrar basılmasını engelle
            progressBar.Value = 0;
            txtTime.Text = "Geçen Süre: Başlıyor...";
            _islemBaslangicZamani = DateTime.Now; // Sayacı sıfırla
            
            EkranaMesajYaz("--- SIKIŞTIRMA BAŞLADI ---");

            try
            {
                // VideoyuSikistirAsync metodu çalışırken arayüzün donmaması için 'await' kullanıyoruz.
                string yeniDosya = await _videoMotoru.VideoyuSikistirAsync(_secilenVideo, _secilenKlasor, seciliAyarlar);
                
                EkranaMesajYaz("--- İŞLEM BİTTİ ---");
                MessageBox.Show("Sıkıştırma tamamlandı!\nKaydedilen Yer:\n" + yeniDosya);
            }
            catch (Exception hata)
            {
                EkranaMesajYaz("HATA OLUŞTU: " + hata.Message);
                MessageBox.Show("İşlem sırasında hata oluştu: " + hata.Message);
            }
            finally
            {
                // İşlem başarılı da olsa hata da verse, butonu tekrar aktif et
                btnStart.IsEnabled = true;
                txtTime.Text = "Durum: Bitti";
            }
        }
    }
}