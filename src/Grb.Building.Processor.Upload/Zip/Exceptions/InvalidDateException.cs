namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Runtime.Serialization;
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public sealed class InvalidDateException : Exception
    {
        public RecordNumber RecordNumber { get; }
        public string DateAsCharacter { get; }

        public InvalidDateException(RecordNumber recordNumber, string dateAsCharacter) : base("")
        {
            RecordNumber = recordNumber;
            DateAsCharacter = dateAsCharacter;
        }
    }
}
