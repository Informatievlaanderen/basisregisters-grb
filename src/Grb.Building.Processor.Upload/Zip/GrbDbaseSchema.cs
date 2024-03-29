﻿namespace Grb.Building.Processor.Upload.Zip
{
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public class GrbDbaseSchema : DbaseSchema
    {
        public DbaseField GRBIDN => Fields[0];
        public DbaseField IDNV => Fields[1];
        public DbaseField GVDV => Fields[2];
        public DbaseField GVDE => Fields[3];
        public DbaseField EventType => Fields[4];
        public DbaseField GRBOBJECT => Fields[5];
        public DbaseField GRID => Fields[6];
        public DbaseField TPC => Fields[7];

        public GrbDbaseSchema() => Fields = new[]
        {
            DbaseField.CreateNumberField(new DbaseFieldName(nameof(GRBIDN)), new DbaseFieldLength(9), new DbaseDecimalCount(0)),
            DbaseField.CreateNumberField(new DbaseFieldName(nameof(IDNV)), new DbaseFieldLength(4), new DbaseDecimalCount(0)),
            DbaseField.CreateCharacterField(new DbaseFieldName(nameof(GVDV)), new DbaseFieldLength(10)),
            DbaseField.CreateCharacterField(new DbaseFieldName(nameof(GVDE)), new DbaseFieldLength(10)),
            DbaseField.CreateNumberField(new DbaseFieldName(nameof(EventType)), new DbaseFieldLength(4), new DbaseDecimalCount(0)),
            DbaseField.CreateNumberField(new DbaseFieldName(nameof(GRBOBJECT)), new DbaseFieldLength(4), new DbaseDecimalCount(0)),
            DbaseField.CreateCharacterField(new DbaseFieldName(nameof(GRID)), new DbaseFieldLength(128)),
            DbaseField.CreateNumberField(new DbaseFieldName(nameof(TPC)), new DbaseFieldLength(4), new DbaseDecimalCount(0)),
        };
    }
}
