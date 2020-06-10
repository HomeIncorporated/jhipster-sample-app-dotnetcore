using System.Collections.Generic;
using System.Threading.Tasks;
using JHipsterNet.Pagination;
using JHipsterNet.Pagination.Extensions;
using MyCompany.Data;
using MyCompany.Data.Extensions;
using MyCompany.Models;
using MyCompany.Web.Extensions;
using MyCompany.Web.Filters;
using MyCompany.Web.Rest.Problems;
using MyCompany.Web.Rest.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace MyCompany.Controllers {
    [Authorize]
    [Route("api")]
    [ApiController]
    public class LocationController : ControllerBase {
        private const string EntityName = "location";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<LocationController> _log;

        public LocationController(ILogger<LocationController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("locations")]
        [ValidateModel]
        public async Task<ActionResult<Location>> CreateLocation([FromBody] Location location)
        {
            _log.LogDebug($"REST request to save Location : {location}");
            if (location.Id != 0)
                throw new BadRequestAlertException("A new location cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(location);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetLocation), new { id = location.Id }, location)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, location.Id.ToString()));
        }

        [HttpPut("locations")]
        [ValidateModel]
        public async Task<IActionResult> UpdateLocation([FromBody] Location location)
        {
            _log.LogDebug($"REST request to update Location : {location}");
            if (location.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(location);
            /* Force the reference navigation property to be in "modified" state.
            This allows to modify it with a null value (the field is nullable).
            This takes into consideration the case of removing the association between the two instances. */
            _applicationDatabaseContext.Entry(location).Reference(location0 => location0.Country).IsModified = true;
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(location)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, location.Id.ToString()));
        }

        [HttpGet("locations")]
        public ActionResult<IEnumerable<Location>> GetAllLocations(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of Locations");
            var page = _applicationDatabaseContext.Locations
                .Include(location => location.Country)
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("locations/{id}")]
        public async Task<IActionResult> GetLocation([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get Location : {id}");
            var result = await _applicationDatabaseContext.Locations
                .Include(location => location.Country)
                .SingleOrDefaultAsync(location => location.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("locations/{id}")]
        public async Task<IActionResult> DeleteLocation([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete Location : {id}");
            _applicationDatabaseContext.Locations.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
