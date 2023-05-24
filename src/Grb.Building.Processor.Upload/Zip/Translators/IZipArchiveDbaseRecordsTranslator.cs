namespace Grb.Building.Processor.Upload.Zip.Translators
{
    using System.Collections.Generic;
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public interface IZipArchiveDbaseRecordsTranslator
    {
        IDictionary<RecordNumber, JobRecord> Translate(IDbaseRecordEnumerator<GrbDbaseRecord> records);
    }
}
