namespace Grb.Building.Processor.Upload.Zip.Translators
{
    using System.Collections.Generic;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
    using Exceptions;

    public sealed class GrbShapeRecordsTranslator : IZipArchiveShapeRecordsTranslator
    {
        public IDictionary<RecordNumber, JobRecord> Translate(IEnumerator<ShapeRecord> records, IDictionary<RecordNumber, JobRecord> jobRecords)
        {
            var matchingShapeWithRecord = jobRecords.ToDictionary(x => x.Key, x => false);

            while (records.MoveNext())
            {
                var shapeRecord = records.Current;
                if (shapeRecord.Content is PolygonShapeContent content)
                {
                    var jobRecord = jobRecords[shapeRecord.Header.RecordNumber];

                    if (jobRecord is not null)
                    {
                        matchingShapeWithRecord[shapeRecord.Header.RecordNumber] = true;
                        jobRecord.Geometry = GeometryTranslator.ToGeometryPolygon(content.Shape);
                    }
                }
            }

            var recordsWithMissingShape = matchingShapeWithRecord.Where(x => x.Value == false);
            if (recordsWithMissingShape.Any())
            {
                throw new DbRecordsWithMissingShapeException(recordsWithMissingShape.Select(x => x.Key.ToInt32()));
            }

            return jobRecords;
        }
    }
}
