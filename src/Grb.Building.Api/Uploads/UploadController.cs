namespace Grb.Building.Api.Uploads
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.Auth.AcmIdm;
    using Be.Vlaanderen.Basisregisters.Api;
    using Handlers;
    using Infrastructure.Query;
    using MediatR;
    using Microsoft.AspNetCore.Authentication.JwtBearer;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [ApiVersion("2.0")]
    [AdvertiseApiVersions("2.0")]
    [ApiRoute("uploads")]
    [ApiExplorerSettings(GroupName = "Upload")]
    public class UploadController : ApiController
    {
        private readonly IMediator _mediator;

        public UploadController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpPost("jobs")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> GetUploadPreSignedUrl(CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new UploadPreSignedUrlRequest(), cancellationToken));
        }

        [HttpGet("jobs/active")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> GetActiveJobs(CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new GetActiveJobsRequest(), cancellationToken));
        }

        [HttpGet("jobs")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> GetJobs(CancellationToken cancellationToken)
        {
            var pagination = new Pagination(HttpContext.Request.Query);
            var statusesFilter = new EnumFilter<JobStatus>(HttpContext.Request.Query, "statuses");

            return Ok(await _mediator.Send(new GetAllJobsRequest(pagination, statusesFilter), cancellationToken));
        }

        [HttpGet("jobs/{jobId:guid}/jobrecords")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> GetJobRecords(Guid jobId, CancellationToken cancellationToken)
        {
            var pagination = new Pagination(HttpContext.Request.Query);
            var statusesFilter = new EnumFilter<JobRecordStatus>(HttpContext.Request.Query, "statuses");

            return Ok(await _mediator.Send(new GetAllJobRecordsRequest(pagination, statusesFilter, jobId), cancellationToken));
        }

        [HttpGet("jobs/{jobId:guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> GetJob(Guid jobId, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new JobRequest(jobId), cancellationToken));
        }

        [HttpGet("jobs/{jobId:guid}/results")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> GetResultsPreSignedUrl(Guid jobId, CancellationToken cancellationToken)
        {
            return Ok(await _mediator.Send(new JobResultsPreSignedUrlRequest(jobId), cancellationToken));
        }

        [HttpDelete("jobs/{jobId:guid}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.GrbBijwerker)]
        public async Task<IActionResult> CancelJob(Guid jobId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new CancelJobRequest(jobId), cancellationToken);
            return NoContent();
        }

        [HttpDelete("jobs/{jobId:guid}/jobrecords/{jobRecordId:long}")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme, Policy = PolicyNames.IngemetenGebouw.InterneBijwerker)]
        public async Task<IActionResult> ResolveJobRecordError(Guid jobId, long jobRecordId, CancellationToken cancellationToken)
        {
            await _mediator.Send(new ResolveJobRecordErrorRequest(jobId, jobRecordId), cancellationToken);
            return NoContent();
        }
    }
}
