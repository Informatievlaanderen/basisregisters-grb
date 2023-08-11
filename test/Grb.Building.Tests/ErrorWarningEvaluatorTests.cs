namespace Grb.Building.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.BasicApiProblem;
    using FluentAssertions;
    using Grb.Building.Processor.Job;
    using TicketingService.Abstractions;
    using Xunit;

    public class ErrorWarningEvaluatorTests
    {
        [Fact]
        public void GivenOnlyErrorsInValidationErrors_ThenOutputError()
        {
            var validationErrors = new List<ValidationError>
            {
                new ValidationError("error1", "reason"),
                new ValidationError("error2", "reason2"),
            };

            // Act
            var (status, code, message) = ErrorWarningEvaluator.Evaluate(validationErrors);

            // Assert
            status.Should().Be(JobRecordStatus.Error);
            code.Should().Be($"error1{Environment.NewLine}error2");
            message.Should().Be($"reason{Environment.NewLine}reason2");
        }

        [Fact]
        public void GivenErrorWithoutCode_ThenOutputError()
        {
            var validationErrors = new List<ValidationError>
            {
                new ValidationError(null, "reason")
            };

            // Act
            var (status, code, message) = ErrorWarningEvaluator.Evaluate(validationErrors);

            // Assert
            status.Should().Be(JobRecordStatus.Error);
            message.Should().Be($"reason");
            code.Should().BeNull();
        }

        [Theory]
        [InlineData("VerwijderdGebouw", "verwijderdeGebouw message")]
        public void GivenOnlyWarningsInValidationErrors_ThenOutputWarning(string code, string message)
        {
            var validationErrors = new List<ValidationError>
            {
                new ValidationError(code, message),
            };

            // Act
            var (status, errorCode, resultMessage) = ErrorWarningEvaluator.Evaluate(validationErrors);

            // Assert
            status.Should().Be(JobRecordStatus.Warning);
            resultMessage.Should().Be(message);
            errorCode.Should().Be(errorCode);
        }

        [Fact]
        public void GivenBothWarningAndErrorsInValidationErrors_ThenOutputError()
        {
            var validationErrors = new List<ValidationError>
            {
                new ValidationError("error1", "reason"),
                new ValidationError(ErrorWarningEvaluator.Warnings.First(), "warning message"),
                new ValidationError("error2", "reason2"),
            };

            // Act
            var (status, code, message) = ErrorWarningEvaluator.Evaluate(validationErrors);

            // Assert
            status.Should().Be(JobRecordStatus.Error);
            message.Should().Be($"reason{Environment.NewLine}reason2");
            code.Should().Be($"error1{Environment.NewLine}error2");
        }

        [Fact]
        public void GivenErrorInTicketError_ThenOutputError()
        {
            var ticketError = new TicketError("reason", "error1");

            // Act
            var (status, evaluatedTicketError) = ErrorWarningEvaluator.Evaluate(ticketError);

            // Assert
            status.Should().Be(JobRecordStatus.Error);
            evaluatedTicketError.ErrorMessage.Should().Be("reason");
        }

        [Theory]
        [InlineData("VerwijderdGebouw", "verwijderdeGebouw message")]
        public void GivenWarningInTicketError_ThenOutputWarning(string code, string message)
        {
            var ticketError = new TicketError(message, code);

            // Act
            var (status, resultMessage) = ErrorWarningEvaluator.Evaluate(ticketError);

            // Assert
            status.Should().Be(JobRecordStatus.Warning);
            resultMessage.ErrorMessage.Should().Be(message);
        }
    }
}
