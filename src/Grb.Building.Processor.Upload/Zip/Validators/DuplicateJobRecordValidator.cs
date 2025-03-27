namespace Grb.Building.Processor.Upload.Zip.Validators
{
    using System.Linq;
    using Dapper;
    using Microsoft.Data.SqlClient;
    using Microsoft.EntityFrameworkCore;

    public interface IDuplicateJobRecordValidator
    {
        public bool HasDuplicateNewBuilding(int idn, int idnVersion, GrbObject grbObject);
    }

    public sealed class DuplicateJobRecordValidator : IDuplicateJobRecordValidator
    {
        private readonly BuildingGrbContext _context;

        public DuplicateJobRecordValidator(BuildingGrbContext context)
        {
            _context = context;
        }

        public bool HasDuplicateNewBuilding(int idn, int idnVersion, GrbObject grbObject)
        {
            var hasDuplicate = _context
                    .JobRecords
                    .Any(x => x.Idn == idn
                                   && x.IdnVersion == idnVersion
                                   && x.GrbObject == grbObject
                                   && x.EventType == GrbEventType.DefineBuilding
                                   && x.GrId == JobRecord.DefaultNewBuildingGrId);

            if (hasDuplicate)
                return true;

            using var connection = new SqlConnection(_context.Database.GetConnectionString());
            connection.Open();

            var sql = $"""
                       SELECT COUNT(*)
                       FROM [{BuildingGrbContext.Schema}].[{JobRecordConfiguration.ArchiveTableName}]
                       WHERE [Idn] = @idn
                         AND [IdnVersion] = @idnVersion
                         AND [GrbObject] = @grbObject
                         AND [EventType] = @eventType
                         AND [GrId] = @grId
                       """;

            var count = connection.ExecuteScalar<int>(sql, new
            {
                idn,
                idnVersion,
                grbObject,
                eventType = GrbEventType.DefineBuilding,
                grId = JobRecord.DefaultNewBuildingGrId
            });

            return count > 0;
        }
    }
}
