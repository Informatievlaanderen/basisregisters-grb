namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System.Collections.Generic;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Exceptions;

    public class GrbShapeRecordsValidator : IZipArchiveShapeRecordsValidator
    {
        public IDictionary<RecordNumber, List<ValidationErrorType>> Validate(
            string zipArchiveEntryName,
            IEnumerator<ShapeRecord> records)
        {
            var validationErrors = new Dictionary<RecordNumber, List<ValidationErrorType>>();

            var moved = records.MoveNext();

            if (!moved)
            {
                throw new NoShapeRecordsException(zipArchiveEntryName);
            }

            while (moved)
            {
                var record = records.Current;
                if (record.Content.ShapeType != ShapeType.Polygon)
                {
                    validationErrors.Add(record.Header.RecordNumber, new List<ValidationErrorType>
                    {
                        ValidationErrorType.GeometryIsNotPolygon
                    });
                }

                moved = records.MoveNext();
            }

            return validationErrors;
        }
    }
}
