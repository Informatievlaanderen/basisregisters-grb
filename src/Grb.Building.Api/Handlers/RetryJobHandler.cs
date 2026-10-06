namespace Grb.Building.Api.Handlers
{
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
    using TicketingService.Abstractions;

    /// <summary>
    /// Puts a job in error back to the point in the process where it failed, so the processors pick it up again.
    /// </summary>
    public sealed class RetryJobHandler : IRequestHandler<RetryJobRequest>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketing _ticketing;

        public RetryJobHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketing ticketing)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketing = ticketing;
        }

        public async Task Handle(RetryJobRequest request, CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
            }

            if (!job.IsInError())
            {
                throw new ApiException(
                    $"De status van de upload job '{request.JobId}' is {job.Status.ToString().ToLower()}, hierdoor kan deze job niet opnieuw verwerkt worden.",
                    StatusCodes.Status400BadRequest);
            }

            var hasJobRecords = await _buildingGrbContext.JobRecords
                .AnyAsync(x => x.JobId == job.Id, cancellationToken);

            var errorRecords = await _buildingGrbContext.JobRecords
                .Where(x => x.JobId == job.Id && x.Status == JobRecordStatus.Error)
                .ToListAsync(cancellationToken);

            if (!hasJobRecords)
            {
                // Failed in the upload processor: validate the received zip again.
                job.UpdateStatus(JobStatus.Created);
            }
            else if (errorRecords.Any())
            {
                // Failed in the job processor: send the failed records to the backoffice again.
                errorRecords.ForEach(x => x.RetryError());
                job.UpdateStatus(JobStatus.Processing);
            }
            else
            {
                // The upload processor failed after the records were stored, but before the job was marked as prepared.
                job.UpdateStatus(JobStatus.Prepared);
            }

            await _ticketing.Pending(job.TicketId!.Value, cancellationToken);
            await _buildingGrbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
