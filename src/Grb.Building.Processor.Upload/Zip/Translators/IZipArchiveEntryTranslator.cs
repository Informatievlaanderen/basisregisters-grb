namespace Grb.Building.Processor.Upload.Zip.Translators
{
    using System.Collections.Generic;
    using System.IO.Compression;
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public interface IZipArchiveEntryTranslator
    {
        IDictionary<RecordNumber, JobRecord> Translate(ZipArchiveEntry entry, IDictionary<RecordNumber, JobRecord> jobRecords);
    }
}
