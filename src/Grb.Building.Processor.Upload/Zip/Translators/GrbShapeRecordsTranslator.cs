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

                        var geometry = GeometryTranslator.ToGeometryPolygon(content.Shape);

                        // The shape file carries no reference system, so it is decided from the coordinates
                        // and recorded on the geometry here - the job is processed later, possibly long
                        // after GRB switches, and by then only the row can say what it holds.
                        // GrbShapeRecordsValidator has already refused the archive if its records disagree
                        // or fall in neither envelope, so this cannot be null. See ADR 0003.
                        geometry.SRID = GrbGeometryReferenceSystem.Of(geometry)!.Value;

                        jobRecord.Geometry = geometry;
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
