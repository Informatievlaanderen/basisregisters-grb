namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Amazon.Runtime.Internal.Transform;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Oslo.Extensions;
    using Be.Vlaanderen.Basisregisters.GrAr.Edit.Validators;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Exceptions;

    public class GrbDbaseRecordsValidator : IZipArchiveDbaseRecordsValidator<GrbDbaseRecord>
    {
        public IDictionary<RecordNumber, List<ValidationErrorType>> Validate(
            string zipArchiveEntryName,
            IEnumerator<GrbDbaseRecord> records)
        {
            ArgumentNullException.ThrowIfNull(records);

            var validationErrors = new Dictionary<RecordNumber, List<ValidationErrorType>>();

            var moved = records.MoveNext();
            if (!moved)
            {
                throw new NoDbaseRecordsException(zipArchiveEntryName);
            }

            var index = 1;
            while (moved)
            {
                var record = records.Current;
                var validationErrorTypes = new List<ValidationErrorType>();

                if (!record.EventType.HasValue ||
                    (record.EventType.HasValue && !Enum.IsDefined(typeof(GrbEventType), record.EventType.Value)))
                {
                    validationErrorTypes.Add(ValidationErrorType.UnknownEventType);
                }

                if (record.GRID.Value != "-9" &&
                    !(OsloPuriValidator.TryParseIdentifier(record.GRID.Value, out var stringId) &&
                      int.TryParse(stringId, out var persistentLocalId) &&
                      persistentLocalId > 0))
                {
                    throw new InvalidGrIdException(new RecordNumber(index), record.GRID.Value);
                }

                if(!record.GVDV.TryGetValueAsDateTime(out _))
                    validationErrorTypes.Add(ValidationErrorType.InvalidVersionDate);

                if(!record.GVDE.TryGetValueAsNullableDateTime(out _) && !string.IsNullOrEmpty(record.GVDE.Value))
                    validationErrorTypes.Add(ValidationErrorType.InvalidEndDate);

                if(validationErrorTypes.Any())
                    validationErrors.Add(new RecordNumber(index), validationErrorTypes);

                moved = records.MoveNext();

                ++index;
            }

            return validationErrors;
        }
    }
}
