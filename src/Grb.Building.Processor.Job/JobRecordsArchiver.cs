namespace Grb.Building.Processor.Job
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Dapper;
    using Microsoft.Data.SqlClient;
    using Microsoft.Extensions.Logging;

    public interface IJobRecordsArchiver
    {
        Task Archive(Guid jobId, CancellationToken ct);
    }

    public class JobRecordsArchiver : IJobRecordsArchiver
    {
        private const int CommandTimeoutInSeconds = 300;

        private readonly string _connectionString;
        private readonly ILogger<JobRecordsArchiver> _logger;

        public JobRecordsArchiver(string connectionString, ILoggerFactory loggerFactory)
        {
            _connectionString = connectionString;
            _logger = loggerFactory.CreateLogger<JobRecordsArchiver>();
        }

        public async Task Archive(Guid jobId, CancellationToken ct)
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync(ct);

            await using var transaction = connection.BeginTransaction();

                try
                {
                    await ArchiveRecords(connection, transaction, jobId, ct);
                    await RemoveRecords(connection, transaction, jobId, ct);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Rolling back archiving of jobRecords for job '{jobId}'.", jobId);
                    transaction.Rollback();
                    throw;
                }

                transaction.Commit();
        }

        private static async Task RemoveRecords(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid jobId,
            CancellationToken ct)
        {
            await connection.ExecuteAsync(new CommandDefinition(
                $"DELETE FROM [{BuildingGrbContext.Schema}].[{JobRecordConfiguration.TableName}] WHERE [JobId] = @jobId;",
                new { jobId },
                transaction,
                CommandTimeoutInSeconds,
                cancellationToken: ct));
        }

        private static async Task ArchiveRecords(
            SqlConnection connection,
            SqlTransaction transaction,
            Guid jobId,
            CancellationToken ct)
        {
            // Records already in the archive are skipped, so archiving a job again (e.g. after a failed attempt) is safe.
            await connection.ExecuteAsync(new CommandDefinition($@"
INSERT INTO [{BuildingGrbContext.Schema}].[{JobRecordConfiguration.ArchiveTableName}]
    ([Id]
    ,[JobId]
    ,[RecordNumber]
    ,[Idn]
    ,[IdnVersion]
    ,[VersionDate]
    ,[EndDate]
    ,[GrbObject]
    ,[GrbObjectType]
    ,[EventType]
    ,[GrId]
    ,[Geometry]
    ,[Overlap]
    ,[Status]
    ,[ErrorCode]
    ,[ErrorMessage]
    ,[BuildingPersistentLocalId]
    ,[TicketId])
SELECT r.[Id]
    ,r.[JobId]
    ,r.[RecordNumber]
    ,r.[Idn]
    ,r.[IdnVersion]
    ,r.[VersionDate]
    ,r.[EndDate]
    ,r.[GrbObject]
    ,r.[GrbObjectType]
    ,r.[EventType]
    ,r.[GrId]
    ,r.[Geometry]
    ,r.[Overlap]
    ,r.[Status]
    ,r.[ErrorCode]
    ,r.[ErrorMessage]
    ,r.[BuildingPersistentLocalId]
    ,r.[TicketId]
FROM [{BuildingGrbContext.Schema}].[{JobRecordConfiguration.TableName}] r
WHERE r.[JobId] = @jobId
  AND NOT EXISTS (
    SELECT 1
    FROM [{BuildingGrbContext.Schema}].[{JobRecordConfiguration.ArchiveTableName}] a
    WHERE a.[Id] = r.[Id])",
                new { jobId },
                transaction,
                CommandTimeoutInSeconds,
                cancellationToken: ct));
        }
    }
}
