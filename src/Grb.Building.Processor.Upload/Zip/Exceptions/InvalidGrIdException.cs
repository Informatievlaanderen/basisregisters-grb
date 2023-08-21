namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;
    using Be.Vlaanderen.Basisregisters.Shaperon;

    [Serializable]
    public sealed class InvalidGrIdException : Exception
    {
        public RecordNumber RecordNumber { get; }
        public string GrId { get; }

        public InvalidGrIdException(RecordNumber recordNumber, string grId) : base("")
        {
            RecordNumber = recordNumber;
            GrId = grId;
        }

        private InvalidGrIdException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
