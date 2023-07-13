namespace Grb.Building.Api.Handlers
{
    using System.Threading;
    using System.Threading.Tasks;
    using Abstractions.Requests;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Grb;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using Microsoft.EntityFrameworkCore;
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

            if (!await CanCancelJob(job))
            {
                throw new ApiException(
                    $"Upload job '{request.JobId}' wordt verwerkt en kan niet worden geannuleerd.",
                    StatusCodes.Status400BadRequest);
            }

            await _ticketing.Complete(
                job.TicketId!.Value,
                new TicketResult(new { JobStatus = "Cancelled" }),
                cancellationToken);

            job.UpdateStatus(JobStatus.Cancelled);
            await _buildingGrbContext.SaveChangesAsync(cancellationToken);
        }

        private async Task<bool> CanCancelJob(Job job)
        {
            if (job.Status == JobStatus.Created)
            {
                return true;
            }

            if (job.Status != JobStatus.Error)
            {
                return false;
            }

            return !await _buildingGrbContext.JobRecords.AnyAsync(x => x.JobId == job.Id);
        }
    }
}
