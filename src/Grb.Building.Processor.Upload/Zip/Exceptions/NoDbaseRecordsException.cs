namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Runtime.Serialization;

    public sealed class NoDbaseRecordsException : Exception
    {
        public string FileName { get; }

        public  NoDbaseRecordsException(string fileName) : base("")
        {
            FileName = fileName;
        }
    }
}
