namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public sealed class DbaseHeaderFormatException : Exception
    {
        public string FileName { get; }

        public DbaseHeaderFormatException(string fileName, Exception innerException) : base("", innerException)
        {
            FileName = fileName;
        }
    }
}
