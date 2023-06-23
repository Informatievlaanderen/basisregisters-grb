namespace Grb.Building.Api.Handlers
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Api.Exceptions;
    using Infrastructure;
    using MediatR;
    using Microsoft.AspNetCore.Http;
    using TicketingService.Abstractions;

    public sealed record GetJobByIdRequest(Guid JobId) : IRequest<JobResponse>;

    public sealed class GetJobByIdHandler
        : IRequestHandler<GetJobByIdRequest, JobResponse>
    {
        private readonly BuildingGrbContext _buildingGrbContext;
        private readonly ITicketingUrl _ticketingUrl;
        private readonly IPagedUriGenerator _pagedUriGenerator;

        public GetJobByIdHandler(
            BuildingGrbContext buildingGrbContext,
            ITicketingUrl ticketingUrl,
            IPagedUriGenerator pagedUriGenerator)
        {
            _buildingGrbContext = buildingGrbContext;
            _ticketingUrl = ticketingUrl;
            _pagedUriGenerator = pagedUriGenerator;
        }

        public async Task<JobResponse> Handle(
            GetJobByIdRequest byIdRequest,
            CancellationToken cancellationToken)
        {
            var job = await _buildingGrbContext.FindJob(byIdRequest.JobId, cancellationToken);

            if (job is null)
            {
                throw new ApiException("Onbestaande upload job.", StatusCodes.Status404NotFound);
            }

            return new JobResponse(
                job.Id,
                TicketUrl: job.TicketId.HasValue ? _ticketingUrl.For(job.TicketId.Value) : null,
                job.Status,
                job.Created,
                job.LastChanged,
                _pagedUriGenerator.FirstPage($"v2/uploads/jobs/{job.Id}/jobrecords"));
        }
    }
}
