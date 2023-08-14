namespace Grb.Building.Processor.Upload.Zip.Exceptions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.Serialization;

    [Serializable]
    public sealed class DbRecordsWithMissingShapeException : Exception
    {
        public List<int> RecordNumbers { get; }

        public DbRecordsWithMissingShapeException(IEnumerable<int> recordNumbers) : base("")
        {
            RecordNumbers = recordNumbers.ToList();
        }

        private DbRecordsWithMissingShapeException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        { }
    }
}
