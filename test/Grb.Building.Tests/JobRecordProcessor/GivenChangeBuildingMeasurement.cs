namespace Grb.Building.Tests.JobRecordProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.BasicApiProblem;
    using BuildingRegistry.Api.BackOffice.Abstractions.Building.Requests;
    using FluentAssertions;
    using Grb.Building.Processor.Job;
    using Microsoft.EntityFrameworkCore;
    using Microsoft.Extensions.Logging.Abstractions;
    using Microsoft.Extensions.Options;
    using Moq;
    using NetTopologySuite.Geometries;
    using NodaTime;
    using Xunit;

    public class GivenChangeBuildingMeasurement
    {
        [Fact]
        public async Task ThenChangeBuildingMeasurementRequestIsSent()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory(canBeDisposed: false).CreateDbContext();
            var backOfficeApiProxy = new Mock<IBackOfficeApiProxy>();
            var mockFactory = new Mock<IDbContextFactory<BuildingGrbContext>>();
            mockFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(buildingGrbContext);

            var job = new Job(DateTimeOffset.Now, JobStatus.Prepared, Guid.NewGuid());
            await buildingGrbContext.Jobs.AddAsync(job);

            var jobRecord = new JobRecord
            {
                JobId = job.Id,
                Status = JobRecordStatus.Created,
                EventType = GrbEventType.ChangeBuildingMeasurement,
                Geometry = (Polygon)GeometryHelper.ValidPolygon,
                GrbObject = GrbObject.ArtWork,
                GrbObjectType = GrbObjectType.MainBuilding,
                GrId = 1,
                Id = 2,
                Idn = 3
            };

            await buildingGrbContext.JobRecords.AddAsync(jobRecord);
            await buildingGrbContext.SaveChangesAsync();

            var ticketId = Guid.NewGuid();
            backOfficeApiProxy
                .Setup(x => x.ChangeBuildingMeasurement(
                    jobRecord.GrId,
                    It.Is<ChangeBuildingMeasurementRequest>(y => y.GrbData.Idn == jobRecord.Idn),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BackOfficeApiResult($"https://ticketing.be/{ticketId}", new List<ValidationError>()));

            var jobRecordsProcessor = new JobRecordsProcessor(
                mockFactory.Object,
                backOfficeApiProxy.Object,
                SystemClock.Instance,
                new OptionsWrapper<OutsideOfficeHoursOptions>(new OutsideOfficeHoursOptions { FromHour = 0, UntilHour = 24 }),
                NullLoggerFactory.Instance);

            //act
            await jobRecordsProcessor.Process(job.Id, false, CancellationToken.None);

            //assert
            backOfficeApiProxy.Verify(x => x.ChangeBuildingMeasurement(
                    jobRecord.GrId,
                    It.Is<ChangeBuildingMeasurementRequest>(y => y.GrbData.Idn == jobRecord.Idn),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            var jobRecordEntity = buildingGrbContext.JobRecords.First(x => x.Id == jobRecord.Id);
            jobRecordEntity.Status.Should().Be(JobRecordStatus.Pending);
            jobRecordEntity.TicketId.Should().Be(ticketId);
        }
    }
}
