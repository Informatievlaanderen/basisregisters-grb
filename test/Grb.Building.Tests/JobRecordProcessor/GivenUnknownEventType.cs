﻿namespace Grb.Building.Tests.JobRecordProcessor
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;
    using Be.Vlaanderen.Basisregisters.BasicApiProblem;
    using BuildingRegistry.Api.BackOffice.Abstractions.Building.Requests;
    using FluentAssertions;
    using Grb.Building.Processor.Job;
    using Microsoft.EntityFrameworkCore;
    using Moq;
    using NetTopologySuite.Geometries;
    using Xunit;

    public class GivenUnknown
    {
        [Fact]
        public async Task ThenThrowsNotImplementedException()
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
                EventType = GrbEventType.Unknown,
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
                .Setup(x => x.MeasureBuilding(
                    jobRecord.GrId,
                    It.Is<MeasureBuildingRequest>(y => y.GrbData.Idn == jobRecord.Idn),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new BackOfficeApiResult($"https://ticketing.be/{ticketId}", new List<ValidationError>()));

            var jobRecordsProcessor = new JobRecordsProcessor(
                mockFactory.Object,
                backOfficeApiProxy.Object);

            //act
            var func = async () => await jobRecordsProcessor.Process(job.Id, CancellationToken.None);

            //assert
            await func.Should().ThrowAsync<NotImplementedException>();
        }
    }
}
