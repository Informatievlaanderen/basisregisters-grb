namespace Grb.Building.Processor.Job
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Be.Vlaanderen.Basisregisters.BasicApiProblem;
    using TicketingService.Abstractions;

    public static class ErrorWarningEvaluator
    {
        public static readonly IReadOnlyCollection<string> Warnings = new[]
        {
            "VerwijderdGebouw"
        };

        public static (JobRecordStatus jobRecordStatus, string code, string message) Evaluate(IList<ValidationError> validationErrors)
        {
            var errors = validationErrors
                .Where(x => x.Code is null || !Warnings.Contains(x.Code))
                .ToList();

            return errors.Any()
                ? (JobRecordStatus.Error,
                    errors.Select(x => x.Code).Aggregate((result, error) => $"{result}{Environment.NewLine}{error}"),
                    errors.Select(x => x.Reason).Aggregate((result, error) => $"{result}{Environment.NewLine}{error}"))
                : (JobRecordStatus.Warning,
                    validationErrors.Select(x => x.Code).Aggregate((result, error) => $"{result}{Environment.NewLine}{error}"),
                    validationErrors.Select(x => x.Reason).Aggregate((warning, result) => $"{result}{Environment.NewLine}{warning}"));
        }

        public static (JobRecordStatus jobRecordStatus, TicketError ticketError) Evaluate(TicketError ticketError)
        {
            return Warnings.Contains(ticketError.ErrorCode)
                ? (JobRecordStatus.Warning, ticketError)
                : (JobRecordStatus.Error, ticketError);
        }
    }
}
