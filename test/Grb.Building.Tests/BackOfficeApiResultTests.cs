﻿namespace Grb.Building.Tests
{
    using System;
    using Be.Vlaanderen.Basisregisters.BasicApiProblem;
    using FluentAssertions;
    using Processor.Job;
    using Xunit;

    public class BackOfficeApiResultTests
    {
        [Fact]
        public void WhenNoValidationErrors_ThenSuccess()
        {
            var resultWithNull = new BackOfficeApiResult("http://myticket.be", null);
            var resultWithEmptyCollection = new BackOfficeApiResult("http://myticket.be", Array.Empty<ValidationError>());

            resultWithNull.IsSuccess.Should().BeTrue();
            resultWithEmptyCollection.IsSuccess.Should().BeTrue();
        }

        [Fact]
        public void WhenValidationErrors_ThenFailure()
        {
            var result = new BackOfficeApiResult("http://myticket.be", new []
            {
                new ValidationError("Reason")
            });

            result.IsSuccess.Should().BeFalse();
        }
    }
}
