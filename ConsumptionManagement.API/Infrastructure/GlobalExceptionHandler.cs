using ConsumptionManagement.API.Models;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace ConsumptionManagement.API.Infrastructure
{
    public sealed class GlobalExceptionHandler : IExceptionHandler
    {
        private readonly ILogger<GlobalExceptionHandler> _logger;

        public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
        {
            _logger = logger;
        }

        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext,
            Exception exception,
            CancellationToken cancellationToken)
        {
            if (exception is OperationCanceledException && cancellationToken.IsCancellationRequested)
            {
                return false;
            }

            if (exception is DbUpdateException dbUpdate && UniqueConstraint.IsViolation(dbUpdate))
            {
                await WriteProblem(
                    httpContext,
                    StatusCodes.Status409Conflict,
                    "Conflict",
                    UniqueConstraint.GetDetail(dbUpdate),
                    cancellationToken);
                return true;
            }

            _logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
            await WriteProblem(
                httpContext,
                StatusCodes.Status500InternalServerError,
                "Server error",
                "An unexpected error occurred.",
                cancellationToken);
            return true;
        }

        private static async Task WriteProblem(
            HttpContext httpContext,
            int status,
            string title,
            string detail,
            CancellationToken cancellationToken)
        {
            httpContext.Response.StatusCode = status;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetailsResponse(title, status, detail, httpContext.Request.Path),
                cancellationToken);
        }

        private sealed record ProblemDetailsResponse(string Title, int Status, string Detail, string Instance);
    }

    internal static class UniqueConstraint
    {
        public const string ReadingConflict = "A reading already exists for this meter at the specified timestamp.";
        public const string MeterConflict = "A meter with this meter number already exists.";

        public static bool IsViolation(Exception exception)
        {
            for (var current = exception; current is not null; current = current.InnerException)
            {
                if (current is SqlException sql && sql.Number is 2601 or 2627)
                {
                    return true;
                }

                if (current.Message.Contains("UNIQUE constraint", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("UNIQUE KEY", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("unique index", StringComparison.OrdinalIgnoreCase)
                    || current.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        public static string GetDetail(DbUpdateException exception)
        {
            var entity = exception.Entries.FirstOrDefault()?.Entity;
            if (entity is MeterReading)
            {
                return ReadingConflict;
            }

            if (entity is Meter)
            {
                return MeterConflict;
            }

            var text = exception.ToString();
            if (text.Contains("MeterReading", StringComparison.OrdinalIgnoreCase)
                || text.Contains("TReadingimeUtc", StringComparison.OrdinalIgnoreCase))
            {
                return ReadingConflict;
            }

            if (text.Contains("MeterNumber", StringComparison.OrdinalIgnoreCase))
            {
                return MeterConflict;
            }

            return "The request conflicts with an existing unique value.";
        }
    }
}
