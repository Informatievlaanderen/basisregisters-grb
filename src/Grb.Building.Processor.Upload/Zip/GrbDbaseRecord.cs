namespace Grb.Building.Processor.Upload.Zip
{
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public class GrbDbaseRecord : DbaseRecord
    {
        public static readonly GrbDbaseSchema Schema = new GrbDbaseSchema();

        public DbaseInt32 IDN { get; }
        public DbaseInt32 IDNV { get; }
        public DbaseCharacter GVDV { get; }
        public DbaseCharacter GVDE { get; }
        public DbaseInt32 EventType { get; }
        public DbaseInt32 GRBOBJECT { get; }
        public DbaseCharacter GRID { get; }
        public DbaseInt32 TPC { get; }

        public GrbDbaseRecord()
        {
            IDN = new DbaseInt32(Schema.GRBIDN);
            IDNV = new DbaseInt32(Schema.IDNV);
            GVDV = new DbaseCharacter(Schema.GVDV, options: new DbaseCharacterOptions("yyyy-MM-dd", "yyyy-MM-dd\\THH:mm:ss%K"));
            GVDE = new DbaseCharacter(Schema.GVDE, options: new DbaseCharacterOptions("yyyy-MM-dd", "yyyy-MM-dd\\THH:mm:ss%K"));
            EventType = new DbaseInt32(Schema.EventType);
            GRBOBJECT = new DbaseInt32(Schema.GRBOBJECT);
            GRID = new DbaseCharacter(Schema.GRID);
            TPC = new DbaseInt32(Schema.TPC);

            Values = new DbaseFieldValue[]
            {
                IDN,
                IDNV,
                GVDV,
                GVDE,
                EventType,
                GRBOBJECT,
                GRID,
                TPC
            };
        }
    }
}
