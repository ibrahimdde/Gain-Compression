namespace FfmpegWrapper.Models
{
    // Inheritance (Kalıtım)
    public class VideoFile : MediaFile
    {
        public VideoFile(string filePath) : base(filePath)
        {
        }

        // Polymorphism (Çok Biçimlilik) - Overriding
        public override string GetMediaDescription()
        {
            return $"Video Dosyası: {FileName}{FileExtension} (Boyut: {SizeInBytes / 1024 / 1024} MB)";
        }
    }
}
