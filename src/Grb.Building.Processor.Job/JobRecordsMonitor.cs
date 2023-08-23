namespace Grb.Building.Processor.Job
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.GrAr.Common.Oslo.Extensions;
    using Microsoft.EntityFrameworkCore;
    using Newtonsoft.Json;
    using TicketingService.Abstractions;

    public interface IJobRecordsMonitor
    {
        Task Monitor(Guid jobId, CancellationToken ct);
    }

    public record ETagResponse(string Location, string ETag);

    public class JobRecordsMonitor : IJobRecordsMonitor
    {
        private readonly IDbContextFactory<BuildingGrbContext> _buildingGrbContextFactory;
        private readonly ITicketing _ticketing;

        public JobRecordsMonitor(
            IDbContextFactory<BuildingGrbContext> buildingGrbContextFactory,
            ITicketing ticketing)
        {
            _buildingGrbContextFactory = buildingGrbContextFactory;
            _ticketing = ticketing;
        }

        public async Task Monitor(Guid jobId, CancellationToken ct)
        {
            int pendingJobRecordsCount;
            await using (var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(ct))
            {
                pendingJobRecordsCount = await buildingGrbContext.JobRecords
                    .CountAsync(x => x.JobId == jobId && x.Status == JobRecordStatus.Pending, cancellationToken: ct);
            }

            while (pendingJobRecordsCount > 0)
            {
                const int chunkSize = 50;
                var numberOfChunks = (int)Math.Ceiling(pendingJobRecordsCount / (decimal)chunkSize);

                await Parallel.ForEachAsync(
                    Enumerable.Range(0, numberOfChunks),
                    new ParallelOptions { MaxDegreeOfParallelism = 10 , CancellationToken = ct},
                    async (index, innerCt) =>
                    {
                        var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(innerCt);
                        var jobRecords = buildingGrbContext.JobRecords
                            .Where(x => x.JobId == jobId && x.Status == JobRecordStatus.Pending)
                            .OrderBy(x => x.Id)
                            .Skip(index * chunkSize)
                            .Take(chunkSize)
                            .ToList();

                        foreach (var jobRecord in jobRecords)
                        {
                            var ticket = await _ticketing.Get(jobRecord.TicketId!.Value, innerCt);

                            switch (ticket!.Status)
                            {
                                case TicketStatus.Created:
                                case TicketStatus.Pending:
                                    break;
                                case TicketStatus.Complete:
                                    var etagResponse =
                                        JsonConvert.DeserializeObject<ETagResponse>(ticket.Result!.ResultAsJson!);
                                    jobRecord.BuildingPersistentLocalId =
                                        etagResponse!.Location.AsIdentifier().Map(int.Parse);
                                    jobRecord.Status = JobRecordStatus.Completed;
                                    break;
                                case TicketStatus.Error:
                                    var ticketError =
                                        JsonConvert.DeserializeObject<TicketError>(ticket.Result!.ResultAsJson!);
                                    var evaluation = ErrorWarningEvaluator.Evaluate(ticketError!);
                                    jobRecord.Status = evaluation.jobRecordStatus;
                                    jobRecord.ErrorCode = evaluation.ticketError.ErrorCode;
                                    jobRecord.ErrorMessage = evaluation.ticketError.ErrorMessage;
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(TicketStatus), ticket.Status, null);
                            }

                            await buildingGrbContext.SaveChangesAsync(innerCt);
                        }
                    });

                await using var buildingGrbContext = await _buildingGrbContextFactory.CreateDbContextAsync(ct);
                pendingJobRecordsCount = await buildingGrbContext.JobRecords
                    .CountAsync(x => x.JobId == jobId && x.Status == JobRecordStatus.Pending, cancellationToken: ct);

                if (pendingJobRecordsCount > 0)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), ct);
                }
            }
        }
    }
}
