﻿namespace Grb.Building.Tests.UploadProcessor
{
    using System;
    using System.IO;
    using System.IO.Compression;

    public class ZipArchiveFixture : IDisposable
    {
        public readonly ZipArchive ZipArchive;
        public readonly FileStream ZipFileStream;

        public ZipArchiveFixture()
        {
            ZipFileStream = new FileStream($"{AppContext.BaseDirectory}/UploadProcessor/gebouw_ALL.zip", FileMode.Open, FileAccess.Read);
            ZipArchive = new ZipArchive(ZipFileStream, ZipArchiveMode.Read, false);
        }

        public void Dispose()
        {
            ZipArchive.Dispose();
            ZipFileStream.Dispose();
        }
    }
}
