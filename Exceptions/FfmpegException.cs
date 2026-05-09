using System;

namespace FfmpegWrapper.Exceptions
{
    // Custom Exception Handling
    public class FfmpegException : Exception
    {
        public FfmpegException(string message) : base(message) { }
        public FfmpegException(string message, Exception innerException) : base(message, innerException) { }
    }
}
