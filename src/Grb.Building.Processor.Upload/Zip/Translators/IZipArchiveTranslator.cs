namespace Grb.Building.Processor.Upload.Zip.Translators
{
    using System.Collections.Generic;
    using System.IO.Compression;

    public interface IZipArchiveTranslator
    {
        IEnumerable<JobRecord> Translate(ZipArchive archive);
    }
}
