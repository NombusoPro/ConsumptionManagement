using ConsumptionManagement.API.Data;
using ConsumptionManagement.API.Dtos;
using ConsumptionManagement.API.Infrastructure;
using ConsumptionManagement.API.Models;
using ConsumptionManagement.API.Services;
using ConsumptionManagement.API.Validation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsumptionManagement.API.Controllers
{
    [ApiController]
    [Route("api/meters/{meterId:int}")]
    public class ReadingsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public ReadingsController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost("readings")]
        [ProducesResponseType(typeof(ReadingDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<ReadingDto>> Create(int meterId, CreateReadingRequest request, CancellationToken cancellationToken)
        {
            if (!await _db.Meters.AnyAsync(meter => meter.Id == meterId, cancellationToken))
            {
                return NotFound(new ProblemBody("Meter not found", StatusCodes.Status404NotFound, $"Meter {meterId} was not found."));
            }

            if (!UtcTimestampParser.TryParse(request.TimestampUtc, out var timestampUtc))
            {
                ModelState.AddModelError(nameof(request.TimestampUtc), UtcTimestampParser.ErrorMessage);
                return ValidationProblem(ModelState);
            }

            var duplicate = await _db.MeterReadings.AnyAsync(
                reading => reading.MeterId == meterId && reading.TReadingimeUtc == timestampUtc,
                cancellationToken);
            if (duplicate)
            {
                return Conflict(new ProblemBody("Conflict", StatusCodes.Status409Conflict, UniqueConstraint.ReadingConflict));
            }

            var reading = new MeterReading
            {
                MeterId = meterId,
                TReadingimeUtc = timestampUtc,
                ReadingSum = request.CumulativeKwh!.Value
            };

            _db.MeterReadings.Add(reading);
            await _db.SaveChangesAsync(cancellationToken);

            return Created($"/api/meters/{meterId}/readings", ToDto(reading));
        }

        [HttpGet("readings")]
        [ProducesResponseType(typeof(IReadOnlyList<ReadingDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<ReadingDto>>> List(
            int meterId,
            [FromQuery] string? fromUtc,
            [FromQuery] string? toUtc,
            CancellationToken cancellationToken)
        {
            if (!await _db.Meters.AnyAsync(meter => meter.Id == meterId, cancellationToken))
            {
                return NotFound(new ProblemBody("Meter not found", StatusCodes.Status404NotFound, $"Meter {meterId} was not found."));
            }

            if (!TryParseOptionalRange(fromUtc, toUtc, out var from, out var to))
            {
                return ValidationProblem(ModelState);
            }

            var query = _db.MeterReadings.AsNoTracking().Where(reading => reading.MeterId == meterId);
            if (from is not null)
            {
                query = query.Where(reading => reading.TReadingimeUtc >= from.Value);
            }

            if (to is not null)
            {
                query = query.Where(reading => reading.TReadingimeUtc <= to.Value);
            }

            var readings = await query
                .OrderBy(reading => reading.TReadingimeUtc)
                .ToListAsync(cancellationToken);

            return Ok(readings.Select(ToDto).ToList());
        }

        [HttpGet("summary")]
        [ProducesResponseType(typeof(ConsumptionSummaryDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ConsumptionSummaryDto>> Summary(
            int meterId,
            [FromQuery] string? fromUtc,
            [FromQuery] string? toUtc,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(fromUtc))
            {
                ModelState.AddModelError(nameof(fromUtc), "fromUtc is required.");
            }

            if (string.IsNullOrWhiteSpace(toUtc))
            {
                ModelState.AddModelError(nameof(toUtc), "toUtc is required.");
            }

            if (!ModelState.IsValid)
            {
                return ValidationProblem(ModelState);
            }

            if (!TryParseOptionalRange(fromUtc, toUtc, out var from, out var to))
            {
                return ValidationProblem(ModelState);
            }

            if (!await _db.Meters.AnyAsync(meter => meter.Id == meterId, cancellationToken))
            {
                return NotFound(new ProblemBody("Meter not found", StatusCodes.Status404NotFound, $"Meter {meterId} was not found."));
            }

            var rangeStart = from!.Value;
            var rangeEnd = to!.Value;
            var readings = await _db.MeterReadings
                .AsNoTracking()
                .Where(reading =>
                    reading.MeterId == meterId
                    && reading.TReadingimeUtc >= rangeStart
                    && reading.TReadingimeUtc <= rangeEnd)
                .OrderBy(reading => reading.TReadingimeUtc)
                .Select(reading => new ConsumptionReading(reading.TReadingimeUtc, reading.ReadingSum))
                .ToListAsync(cancellationToken);

            var summary = ConsumptionCalculator.Compute(readings, rangeStart, rangeEnd);
            return Ok(summary);
        }

        private bool TryParseOptionalRange(string? fromUtc, string? toUtc, out DateTime? from, out DateTime? to)
        {
            from = null;
            to = null;

            if (!string.IsNullOrWhiteSpace(fromUtc))
            {
                if (!UtcTimestampParser.TryParse(fromUtc, out var parsedFrom))
                {
                    ModelState.AddModelError(nameof(fromUtc), UtcTimestampParser.ErrorMessage);
                }
                else
                {
                    from = parsedFrom;
                }
            }

            if (!string.IsNullOrWhiteSpace(toUtc))
            {
                if (!UtcTimestampParser.TryParse(toUtc, out var parsedTo))
                {
                    ModelState.AddModelError(nameof(toUtc), UtcTimestampParser.ErrorMessage);
                }
                else
                {
                    to = parsedTo;
                }
            }

            if (from is not null && to is not null && from.Value > to.Value)
            {
                ModelState.AddModelError(nameof(fromUtc), "fromUtc must be less than or equal to toUtc.");
            }

            return ModelState.IsValid;
        }

        private static ReadingDto ToDto(MeterReading reading) =>
            new(
                reading.Id,
                reading.MeterId,
                DateTime.SpecifyKind(reading.TReadingimeUtc, DateTimeKind.Utc),
                reading.ReadingSum);

        private sealed record ProblemBody(string Title, int Status, string Detail);
    }
}
