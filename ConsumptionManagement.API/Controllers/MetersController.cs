using ConsumptionManagement.API.Data;
using ConsumptionManagement.API.Dtos;
using ConsumptionManagement.API.Infrastructure;
using ConsumptionManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsumptionManagement.API.Controllers
{
    [ApiController]
    [Route("api/sites/{siteId:int}/meters")]
    public class MetersController : ControllerBase
    {
        private readonly AppDbContext _db;

        public MetersController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [ProducesResponseType(typeof(MeterDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<ActionResult<MeterDto>> Create(int siteId, CreateMeterRequest request, CancellationToken cancellationToken)
        {
            if (!await _db.Sites.AnyAsync(site => site.Id == siteId, cancellationToken))
            {
                return SiteNotFound(siteId);
            }

            var meterNumber = request.MeterNumber!.Trim();
            var duplicate = await _db.Meters.AnyAsync(
                meter => meter.MeterNumber.ToLower() == meterNumber.ToLower(),
                cancellationToken);
            if (duplicate)
            {
                return Conflict(new ProblemBody("Conflict", StatusCodes.Status409Conflict, UniqueConstraint.MeterConflict));
            }

            var meter = new Meter
            {
                SiteId = siteId,
                MeterNumber = meterNumber
            };

            _db.Meters.Add(meter);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = new MeterDto(meter.Id, meter.SiteId, meter.MeterNumber);
            return Created($"/api/sites/{siteId}/meters", dto);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<MeterDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IReadOnlyList<MeterDto>>> List(int siteId, CancellationToken cancellationToken)
        {
            if (!await _db.Sites.AnyAsync(site => site.Id == siteId, cancellationToken))
            {
                return SiteNotFound(siteId);
            }

            var meters = await _db.Meters
                .AsNoTracking()
                .Where(meter => meter.SiteId == siteId)
                .OrderBy(meter => meter.Id)
                .Select(meter => new MeterDto(meter.Id, meter.SiteId, meter.MeterNumber))
                .ToListAsync(cancellationToken);

            return Ok(meters);
        }

        private ActionResult SiteNotFound(int siteId) =>
            NotFound(new ProblemBody("Site not found", StatusCodes.Status404NotFound, $"Site {siteId} was not found."));

        private sealed record ProblemBody(string Title, int Status, string Detail);
    }
}
