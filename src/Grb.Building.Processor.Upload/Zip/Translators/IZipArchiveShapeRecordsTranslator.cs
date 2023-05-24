namespace Grb.Building.Processor.Upload.Zip.Translators
{
    using System.Collections.Generic;
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public interface IZipArchiveShapeRecordsTranslator
    {
        IDictionary<RecordNumber, JobRecord> Translate(IEnumerator<ShapeRecord> records, IDictionary<RecordNumber, JobRecord> jobRecords);
    }
}

