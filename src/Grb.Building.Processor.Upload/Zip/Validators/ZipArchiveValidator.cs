namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System;
    using System.Collections.Generic;
    using System.IO.Compression;
    using System.Linq;
    using Core;
    using Exceptions;

    public class ZipArchiveValidator : IZipArchiveValidator
    {
        private static readonly string[] ValidationOrder =
        {
            ZipArchiveConstants.DBF_FILENAME.ToUpperInvariant(),
            ZipArchiveConstants.SHP_FILENAME.ToUpperInvariant(),
        };

        private readonly Dictionary<string, IZipArchiveEntryValidator> _validators;

        public ZipArchiveValidator(Dictionary<string, IZipArchiveEntryValidator> archiveEntries)
        {
            _validators = archiveEntries;
        }

        public ZipArchiveProblems Validate(ZipArchive archive)
        {
            ArgumentNullException.ThrowIfNull(archive);

            var problems = ZipArchiveProblems.None;

            // Report all missing required files
            var missingRequiredFiles = new HashSet<string>(
                _validators.Keys,
                StringComparer.InvariantCultureIgnoreCase
            );
            missingRequiredFiles.ExceptWith(
                new HashSet<string>(
                    archive.Entries.Select(entry => entry.FullName),
                    StringComparer.InvariantCultureIgnoreCase
                )
            );
            problems = missingRequiredFiles.Aggregate(
                problems,
                (current, file) =>   current.RequiredFileMissing(file));

            // Validate all required files (if a validator was registered for it)
            try
            {
                if (missingRequiredFiles.Count == 0)
                {
                    var requiredFiles = new HashSet<string>(
                        archive.Entries.Select(entry => entry.FullName),
                        StringComparer.InvariantCultureIgnoreCase
                    );
                    requiredFiles.IntersectWith(
                        new HashSet<string>(
                            _validators.Keys,
                            StringComparer.InvariantCultureIgnoreCase
                        )
                    );

                    foreach (var file in requiredFiles.OrderBy(file =>
                                 Array.IndexOf(ValidationOrder, file.ToUpperInvariant())))
                    {
                        if (_validators.TryGetValue(file, out var validator))
                        {
                            var fileProblems = validator.Validate(archive.GetEntry(file));
                            var fileProblemsGrouped = fileProblems
                                .SelectMany(kvp =>
                                    kvp.Value.Select(errorType =>
                                        new { ErrorType = errorType, RecordNumber = kvp.Key }))
                                .GroupBy(x => x.ErrorType)
                                .ToDictionary(group => group.Key, group => group.Select(x => x.RecordNumber).ToList());

                            foreach (var kvp in fileProblemsGrouped)
                            {
                                problems = problems.Add(new FileError(file, kvp.Key.ToString(),
                                    new ProblemParameter("recordnumbers", string.Join(",", kvp.Value))));
                            }
                        }
                    }
                }
            }
            catch (InvalidGrIdException ex)
            {
                problems += new FileError("GebouwIdOngeldig", $"De meegegeven waarde in de kolom 'GRID' is ongeldig. Record nummer: {ex.RecordNumber}, GRID: {ex.GrId}");
            }
            catch (ShapeHeaderFormatException ex)
            {
                problems += new FileError(ex.FileName, nameof(ShapeHeaderFormatException),
                    new ProblemParameter("Exception", ex.InnerException!.ToString()));
            }
            catch (DbaseHeaderFormatException ex)
            {
                problems += new FileError(ex.FileName, nameof(DbaseHeaderFormatException),
                    new ProblemParameter("Exception", ex.InnerException!.ToString()));
            }
            catch (DbaseHeaderSchemaMismatchException)
            {
                problems += new FileError(nameof(DbaseHeaderSchemaMismatchException), "De kolomnamen komen niet overeen met de verwachte kolomstructuur.");
            }
            catch (NoDbaseRecordsException ex)
            {
                problems += new FileError("DbaseRecordFileLeeg", $"De meegegeven dbase record file ({ex.FileName}) is leeg.");
            }
            catch (NoShapeRecordsException ex)
            {
                problems += new FileError("ShapefileLeeg", $"De meegegeven shapefile ({ex.FileName}) is leeg.");
            }

            return problems;
        }
    }
}
