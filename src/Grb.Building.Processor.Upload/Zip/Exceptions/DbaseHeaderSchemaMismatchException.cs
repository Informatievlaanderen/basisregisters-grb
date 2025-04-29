namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public sealed class DbaseHeaderSchemaMismatchException : Exception
    {
        public string FileName { get; }

        public DbaseHeaderSchemaMismatchException(string fileName) : base("")
        {
            FileName = fileName;
        }
    }
}
