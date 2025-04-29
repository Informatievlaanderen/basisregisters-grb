namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public sealed class ShapeHeaderFormatException : Exception
    {
        public string FileName { get; }

        public ShapeHeaderFormatException(string fileName, Exception innerException) : base("", innerException)
        {
            FileName = fileName;
        }
    }
}
