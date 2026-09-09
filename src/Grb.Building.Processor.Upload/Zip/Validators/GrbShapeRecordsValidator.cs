namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System.Collections.Generic;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Be.Vlaanderen.Basisregisters.Shaperon.Geometries;
    using Exceptions;

    public class GrbShapeRecordsValidator : IZipArchiveShapeRecordsValidator
    {
        public IDictionary<RecordNumber, List<ValidationErrorType>> Validate(
            string zipArchiveEntryName,
            IEnumerator<ShapeRecord> records)
        {
            var validationErrors = new Dictionary<RecordNumber, List<ValidationErrorType>>();

            // Which reference system each record's coordinates put it in, so the archive as a whole can be
            // checked for consistency once every record has been seen.
            var referenceSystems = new Dictionary<RecordNumber, int>();

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
                // else if (!GeometryValidator.IsValid(GeometryTranslator.ToGeometryPolygon((record.Content as PolygonShapeContent)!.Shape)))
                // {
                //     validationErrors.Add(record.Header.RecordNumber, new List<ValidationErrorType>
                //     {
                //         ValidationErrorType.PolygonNotValid
                //     });
                // }
                else
                {
                    var geometry = GeometryTranslator.ToGeometryPolygon((record.Content as PolygonShapeContent)!.Shape);
                    var referenceSystem = GrbGeometryReferenceSystem.Of(geometry);

                    if (referenceSystem is null)
                    {
                        // In neither envelope, so there is no saying what the coordinates mean. Refused
                        // rather than guessed at: a wrong guess reaches building-registry as a building
                        // ~500 km from where it is, with nothing anywhere reporting a problem.
                        validationErrors.Add(record.Header.RecordNumber, new List<ValidationErrorType>
                        {
                            ValidationErrorType.GeometryOutsideFlanders
                        });
                    }
                    else
                    {
                        referenceSystems.Add(record.Header.RecordNumber, referenceSystem.Value);
                    }
                }

                moved = records.MoveNext();
            }

            AddMixedReferenceSystemErrors(referenceSystems, validationErrors);

            return validationErrors;
        }

        /// <summary>
        /// A delivery is in one reference system or the other, never both: GRB converts as a whole. A mix
        /// means something went wrong upstream, not that the archive needs interpreting record by record.
        /// </summary>
        /// <remarks>
        /// The records in the minority are the ones reported, so the operator is pointed at the handful that
        /// disagree rather than at the whole file.
        /// </remarks>
        private static void AddMixedReferenceSystemErrors(
            IDictionary<RecordNumber, int> referenceSystems,
            IDictionary<RecordNumber, List<ValidationErrorType>> validationErrors)
        {
            var bySystem = referenceSystems
                .GroupBy(x => x.Value)
                .OrderByDescending(x => x.Count())
                .ToList();

            if (bySystem.Count < 2)
            {
                return;
            }

            foreach (var recordNumber in bySystem.Skip(1).SelectMany(x => x).Select(x => x.Key))
            {
                if (validationErrors.TryGetValue(recordNumber, out var errors))
                {
                    errors.Add(ValidationErrorType.MixedReferenceSystems);
                }
                else
                {
                    validationErrors.Add(recordNumber, new List<ValidationErrorType>
                    {
                        ValidationErrorType.MixedReferenceSystems
                    });
                }
            }
        }
    }

    // public static class GeometryValidator
    // {
    //     public static bool IsValid(Geometry geometry)
    //     {
    //         var validOp =
    //             new NetTopologySuite.Operation.Valid.IsValidOp(geometry)
    //             {
    //                 IsSelfTouchingRingFormingHoleValid = true
    //             };
    //
    //         return validOp.IsValid;
    //     }
    // }
}
