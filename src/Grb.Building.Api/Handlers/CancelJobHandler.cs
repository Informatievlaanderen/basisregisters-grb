namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using TicketingService.Abstractions;

    public sealed class CancelJobHandler : IRequestHandler<CancelJobRequest>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketing _ticketing;

        public CancelJobHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketing ticketing)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketing = ticketing;
        }

        public async Task Handle(CancelJobRequest request, CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(request.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
            }

            void ThrowCancelException() => throw new ApiException(
                $"De status van de upload job '{request.JobId}' is {job.Status.ToString().ToLower()}, hierdoor kan deze job niet geannuleerd worden.",
                StatusCodes.Status400BadRequest);

            if(job.Status == JobStatus.Error && HasJobRecords(job.Id))
            {
                ThrowCancelException();
            }

            if (!new []{ JobStatus.Created, JobStatus.Cancelled, JobStatus.Error}.Contains(job.Status))
            {
                ThrowCancelException();
            }

            var jobRecordErrors = _buildingGrbContext
                .JobRecords
                .Where(x => x.JobId == job.Id)
                .Select(x => new { ErrorMessage = x.ErrorMessage, ErrorCode = x.ErrorCode})
                .ToList();

                await _ticketing.Complete(
                    job.TicketId!.Value,
                    jobRecordErrors.Any()
                        ? new TicketResult(new {JobStatus = "Cancelled", Errors = jobRecordErrors})
                        : new TicketResult(new { JobStatus = "Cancelled"}),
                cancellationToken);

            job.UpdateStatus(JobStatus.Cancelled);
            await _buildingGrbContext.SaveChangesAsync(cancellationToken);
        }

        private bool HasJobRecords(Guid jobId) =>
            _buildingGrbContext.JobRecords.Any(x => x.JobId == jobId);
    }
}
