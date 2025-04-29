namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public sealed class NoShapeRecordsException : Exception
    {
        public string FileName { get; }

        public  NoShapeRecordsException(string fileName) : base("")
        {
            FileName = fileName;
        }
    }
}
