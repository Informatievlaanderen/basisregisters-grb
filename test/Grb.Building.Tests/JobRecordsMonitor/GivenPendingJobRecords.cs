namespace Grb.Building.Tests.JobRecordsMonitor
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using Grb.Building.Processor.Job;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using NetTopologySuite.Geometries;
    using TicketingService.Abstractions;
    using Xunit;

    public class GivenPendingJobRecords
    {
        [Fact]
        public async Task ThenJobRecordIsCompleted()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory(canBeDisposed: false).CreateDbContext();
            var mockFactory = new Mock<IDbContextFactory<BuildingGrbContext>>();
            mockFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(buildingGrbContext);
            var ticketing = new Mock<ITicketing>();

            var job = new Job(DateTimeOffset.Now, JobStatus.Prepared, Guid.NewGuid());
            await buildingGrbContext.Jobs.AddAsync(job);

            var jobRecord = CreateJobRecord(job.Id, 1);

            await buildingGrbContext.JobRecords.AddAsync(jobRecord);
            await buildingGrbContext.SaveChangesAsync();
            var buildingPersistentLocalId = 11111;
            ticketing
                .Setup(x => x.Get(jobRecord.TicketId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Ticket(jobRecord.TicketId!.Value, TicketStatus.Complete,
                    new Dictionary<string, string>(),
                    new TicketResult(new ETagResponse($"https://building.be/{buildingPersistentLocalId}", "etag"))));

            var monitor = new JobRecordsMonitor(mockFactory.Object, ticketing.Object);

            //act
            await monitor.Monitor(job.Id, CancellationToken.None);

            //assert
            var jobRecordEntity = buildingGrbContext.JobRecords.First(x => x.Id == jobRecord.Id);
            jobRecordEntity.Status.Should().Be(JobRecordStatus.Completed);
            jobRecordEntity.BuildingPersistentLocalId.Should().Be(buildingPersistentLocalId);
        }

        [Fact]
        public async Task ThenRetryUntilJobRecordIsCompleted()
        {
            var buildingGrbContext = new FakeBuildingGrbContextFactory(canBeDisposed: false).CreateDbContext();
            var mockFactory = new Mock<IDbContextFactory<BuildingGrbContext>>();
            mockFactory.Setup(x => x.CreateDbContextAsync(It.IsAny<CancellationToken>())).ReturnsAsync(buildingGrbContext);
            var ticketing = new Mock<ITicketing>();

            var job = new Job(DateTimeOffset.Now, JobStatus.Prepared, Guid.NewGuid());
            await buildingGrbContext.Jobs.AddAsync(job);

            var jobRecord = CreateJobRecord(job.Id, 1);

            await buildingGrbContext.JobRecords.AddAsync(jobRecord);
            await buildingGrbContext.SaveChangesAsync();

            var buildingPersistentLocalId = 11111;
            ticketing
                .SetupSequence(x => x.Get(jobRecord.TicketId!.Value, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Ticket(jobRecord.TicketId!.Value, TicketStatus.Created,
                    new Dictionary<string, string>()))
                .ReturnsAsync(new Ticket(jobRecord.TicketId!.Value, TicketStatus.Pending,
                    new Dictionary<string, string>()))
                .ReturnsAsync(new Ticket(jobRecord.TicketId!.Value, TicketStatus.Complete,
                    new Dictionary<string, string>(),
                    new TicketResult(new ETagResponse($"https://building.be/{buildingPersistentLocalId}", "etag"))));

            var monitor = new JobRecordsMonitor(mockFactory.Object, ticketing.Object);

            //act
            await monitor.Monitor(job.Id, CancellationToken.None);

            //assert
            var jobRecordEntity = buildingGrbContext.JobRecords.First(x => x.Id == jobRecord.Id);
            jobRecordEntity.Status.Should().Be(JobRecordStatus.Completed);
            jobRecordEntity.BuildingPersistentLocalId.Should().Be(buildingPersistentLocalId);

            ticketing.Verify(x => x.Get(jobRecord.TicketId!.Value, It.IsAny<CancellationToken>()), Times.Exactly(3));
        }


        private JobRecord CreateJobRecord(Guid jobId, int id)
        {
            return new JobRecord
            {
                JobId = jobId,
                Status = JobRecordStatus.Pending,
                EventType = GrbEventType.DefineBuilding,
                Geometry = (Polygon) GeometryHelper.ValidPolygon,
                GrbObject = GrbObject.ArtWork,
                GrbObjectType = GrbObjectType.MainBuilding,
                GrId = 1,
                Id = id,
                Idn = 3,
                TicketId = Guid.NewGuid()
            };
        }
    }
}
