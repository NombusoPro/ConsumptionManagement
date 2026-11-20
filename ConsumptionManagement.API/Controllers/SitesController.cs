using ConsumptionManagement.API.Data;
using ConsumptionManagement.API.Dtos;
using ConsumptionManagement.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ConsumptionManagement.API.Controllers
{
    [ApiController]
    [Route("api/sites")]
    public class SitesController : ControllerBase
    {
        private readonly AppDbContext _db;

        public SitesController(AppDbContext db)
        {
            _db = db;
        }

        [HttpPost]
        [ProducesResponseType(typeof(SiteDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SiteDto>> Create(CreateSiteRequest request, CancellationToken cancellationToken)
        {
            var site = new Site
            {
                Name = request.Name!.Trim(),
                Address = string.IsNullOrWhiteSpace(request.Address) ? null : request.Address.Trim()
            };

            _db.Sites.Add(site);
            await _db.SaveChangesAsync(cancellationToken);

            var dto = new SiteDto(site.Id, site.Name, site.Address);
            return Created($"/api/sites/{site.Id}", dto);
        }

        [HttpGet]
        [ProducesResponseType(typeof(IReadOnlyList<SiteDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IReadOnlyList<SiteDto>>> List(CancellationToken cancellationToken)
        {
            var sites = await _db.Sites
                .AsNoTracking()
                .OrderBy(site => site.Id)
                .Select(site => new SiteDto(site.Id, site.Name, site.Address))
                .ToListAsync(cancellationToken);

            return Ok(sites);
        }
    }
}
