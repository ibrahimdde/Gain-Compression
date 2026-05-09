using System.IO;

namespace FfmpegWrapper.Models
{
    // Abstraction (Soyutlama) ve Base Class (Temel Sınıf)
    public abstract class MediaFile
    {
        public string FilePath { get; protected set; }
        public string FileName { get; protected set; }
        public string FileExtension { get; protected set; }
        public long SizeInBytes { get; protected set; }

        protected MediaFile(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Dosya bulunamadı.", filePath);

            FilePath = filePath;
            FileName = Path.GetFileNameWithoutExtension(filePath);
            FileExtension = Path.GetExtension(filePath);
            SizeInBytes = new FileInfo(filePath).Length;
        }

        // Polymorphism (Çok Biçimlilik) için sanal metot
        public abstract string GetMediaDescription();
    }
}
