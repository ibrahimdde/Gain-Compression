using System.IO;

namespace FfmpegWrapper.Models
{
    // Abstraction (Soyutlama) ve Base Class (Temel Sınıf)
    public abstract class MediaFile
    {
        // Gizli (Private) Alanlar
        private string _filePath;
        private string _fileName;
        private string _fileExtension;
        private long _sizeInBytes;

        // Dışarıdan sadece okumaya açık, içeriden değiştirilebilir (Encapsulation)
        public string FilePath
        {
            get { return _filePath; }
            protected set { _filePath = value; }
        }

        public string FileName
        {
            get { return _fileName; }
            protected set { _fileName = value; }
        }

        public string FileExtension
        {
            get { return _fileExtension; }
            protected set { _fileExtension = value; }
        }

        public long SizeInBytes
        {
            get { return _sizeInBytes; }
            protected set { _sizeInBytes = value; }
        }

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
