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

        public ZipArchiveValidator(
            Dictionary<string, IZipArchiveEntryValidator> archiveEntries)
        {
            _validators = archiveEntries;
        }

        public ZipArchiveProblems Validate(ZipArchive archive)
        {
            ArgumentNullException.ThrowIfNull(archive);

            var problems = ZipArchiveProblems.None;

            var missingRequiredFiles = FindMissingRequiredFiles(archive);
            if (missingRequiredFiles.Any())
            {
                problems = missingRequiredFiles.Aggregate(
                    problems,
                    (current, file) => current.RequiredFileMissing(file));

                return problems;
            }

            try
            {
                var requiredFiles = GetRequiredFiles(archive);
                requiredFiles.IntersectWith(
                    new HashSet<string>(
                        _validators.Keys,
                        StringComparer.InvariantCultureIgnoreCase
                    )
                );

                foreach (var file in requiredFiles
                             .OrderBy(file => Array.IndexOf(ValidationOrder, file.ToUpperInvariant())))
                {
                    if (!_validators.TryGetValue(file, out var validator))
                    {
                        continue;
                    }

                    var problemsInFile = validator.Validate(archive.GetEntry(file));
                    var problemsInFileByErrorType = problemsInFile
                        .SelectMany(kvp =>
                        {
                            var (recordNumber, errorTypes) = kvp;
                            return errorTypes.Select(errorType => new
                            {
                                ErrorType = errorType,
                                RecordNumber = recordNumber
                            });
                        })
                        .GroupBy(x => x.ErrorType)
                        .ToDictionary(
                            group => group.Key,
                            group => group.Select(x => x.RecordNumber).ToList());

                    foreach (var problemsForErrorType in problemsInFileByErrorType)
                    {
                        problems.Add(new FileError(
                            file,
                            // I think we should translate below to a proper string/text as this will be showed in the job ticket error result.
                            problemsForErrorType.Key.ToString(),
                            new ProblemParameter("recordNumbers", string.Join(",", problemsForErrorType.Value))));
                    }
                }
            }
            catch (ShapeHeaderFormatException ex)
            {
                problems += new FileError(
                    ex.FileName,
                    // I think we should translate below to a proper string/text as this will be showed in the job ticket error result.
                    nameof(ShapeHeaderFormatException),
                    new ProblemParameter("Exception", ex.InnerException!.ToString()));
            }
            catch (DbaseHeaderFormatException ex)
            {
                problems += new FileError(
                    ex.FileName,
                    // I think we should translate below to a proper string/text as this will be showed in the job ticket error result.
                    nameof(DbaseHeaderFormatException),
                    new ProblemParameter("Exception", ex.InnerException!.ToString()));
            }
            catch (DbaseHeaderSchemaMismatchException ex)
            {
                // I think we should translate below to a proper string/text as this will be showed in the job ticket error result.
                problems += new FileError(ex.FileName, nameof(DbaseHeaderSchemaMismatchException));
            }
            catch (NoDbaseRecordsException ex)
            {
                // I think we should translate below to a proper string/text as this will be showed in the job ticket error result.
                problems += new FileError(ex.FileName, nameof(NoDbaseRecordsException));
            }

            return problems;
        }

        private HashSet<string> FindMissingRequiredFiles(ZipArchive archive)
        {
            var missingRequiredFiles = new HashSet<string>(_validators.Keys, StringComparer.InvariantCultureIgnoreCase);

            missingRequiredFiles.ExceptWith(GetRequiredFiles(archive));

            return missingRequiredFiles;
        }

        private static HashSet<string> GetRequiredFiles(ZipArchive archive)
        {
            return new HashSet<string>(
                archive.Entries.Select(entry => entry.FullName),
                StringComparer.InvariantCultureIgnoreCase);
        }
    }
}
