namespace Grb
{
    using Be.Vlaanderen.Basisregisters.Shaperon;

    public class JobResultDbaseSchema : DbaseSchema
    {
        public DbaseField GrbIdn => Fields[0];
        public DbaseField GrbObject => Fields[1];
        public DbaseField GrId => Fields[2];


        public JobResultDbaseSchema() => Fields = new[]
        {
            DbaseField.CreateNumberField(new DbaseFieldName("Grbidn"), new DbaseFieldLength(10), new DbaseDecimalCount(0)),
            DbaseField.CreateNumberField(new DbaseFieldName("Grbobject"), new DbaseFieldLength(10), new DbaseDecimalCount(0)),
            DbaseField.CreateCharacterField(new DbaseFieldName("GR-id"), new DbaseFieldLength(100))
        };
    }
}
