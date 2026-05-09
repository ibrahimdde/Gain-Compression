namespace FfmpegWrapper.Models
{
    // Kalıtım (Inheritance): VideoFile sınıfı, MediaFile sınıfının özelliklerini (dosya adı, boyutu vb.) miras alır.
    // Yani bir video dosyası, aynı zamanda genel bir medya dosyasıdır.
    public class VideoFile : MediaFile
    {
        // Yeni bir video oluşturulurken girilen dosya yolunu (yol), ana sınıfa (base) iletiyoruz.
        public VideoFile(string yol) : base(yol)
        {
        }

        // Ana sınıftaki açıklamayı kendimize göre değiştiriyoruz (Override - Ezme).
        public override string AciklamaGetir()
        {
            // MB cinsinden boyutu bulmak için 1024'e bölüyoruz.
            long boyutMB = Boyut / 1024 / 1024;
            return $"Video Dosyası: {DosyaAdi}{Uzanti} (Boyut: {boyutMB} MB)";
        }
    }
}
