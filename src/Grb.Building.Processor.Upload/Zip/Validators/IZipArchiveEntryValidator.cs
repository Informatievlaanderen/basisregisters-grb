namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System.Collections.Generic;
    using System.IO.Compression;
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public interface IZipArchiveEntryValidator
    {
        IDictionary<RecordNumber, List<ValidationErrorType>> Validate(ZipArchiveEntry? entry);
    }
}
