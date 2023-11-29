namespace Grb.Building.Processor.Upload.Zip.Translators
{
    using System;
    using System.Collections.Generic;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Oslo.Extensions;
    using Be.Vlaanderen.Basisregisters.GrAr.Edit.Validators;
    using Be.Vlaanderen.Basisregisters.Shaperon;
    using Exceptions;

    public class GrbDbaseRecordsTranslator : IZipArchiveDbaseRecordsTranslator
    {
        public IDictionary<RecordNumber, JobRecord> Translate(IDbaseRecordEnumerator<GrbDbaseRecord> records)
        {
            var jobRecords = new Dictionary<RecordNumber, JobRecord>();

            var recordNumber = RecordNumber.Initial;

            while (records.MoveNext())
            {
                var record = records.Current;

                var grId = record.GRID.Value == "-9"
                    ? -9
                    : OsloPuriValidator.TryParseIdentifier(record.GRID.Value, out var stringId) && int.TryParse(stringId, out int persistentLocalId) && persistentLocalId > 0
                        ? persistentLocalId
                        : throw new InvalidGrIdException(recordNumber, record.GRID.Value);

                if(!record.GVDV.TryGetValueAsDateTime(out var versionDate))
                    throw new InvalidDateException(recordNumber, record.GVDV.Value);

                if(!record.GVDE.TryGetValueAsNullableDateTime(out var endDate) && !string.IsNullOrEmpty(record.GVDE.Value))
                    throw new InvalidDateException(recordNumber, record.GVDE.Value);

                jobRecords.Add(recordNumber, new JobRecord
                {
                    RecordNumber = recordNumber.ToInt32(),
                    Idn = record.IDN.Value,
                    IdnVersion = record.IDNV.Value,
                    VersionDate = versionDate,
                    EndDate = endDate, // GRB will always send 1990-01-01
                    EventType = (GrbEventType)record.EventType.Value,
                    GrbObject = (GrbObject)record.GRBOBJECT.Value,
                    GrId = grId,
                    GrbObjectType = (GrbObjectType)record.TPC.Value,
                    Status = JobRecordStatus.Created
                });

                recordNumber = recordNumber.Next();
            }

            return jobRecords;
        }
    }
}
