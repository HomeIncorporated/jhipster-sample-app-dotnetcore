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
    public class RegionController : ControllerBase {
        private const string EntityName = "region";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<RegionController> _log;

        public RegionController(ILogger<RegionController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("regions")]
        [ValidateModel]
        public async Task<ActionResult<Region>> CreateRegion([FromBody] Region region)
        {
            _log.LogDebug($"REST request to save Region : {region}");
            if (region.Id != 0)
                throw new BadRequestAlertException("A new region cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.Regions.Add(region);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetRegion), new { id = region.Id }, region)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, region.Id.ToString()));
        }

        [HttpPut("regions")]
        [ValidateModel]
        public async Task<IActionResult> UpdateRegion([FromBody] Region region)
        {
            _log.LogDebug($"REST request to update Region : {region}");
            if (region.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(region);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(region)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, region.Id.ToString()));
        }

        [HttpGet("regions")]
        public ActionResult<IEnumerable<Region>> GetAllRegions(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of Regions");
            var page = _applicationDatabaseContext.Regions
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("regions/{id}")]
        public async Task<IActionResult> GetRegion([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get Region : {id}");
            var result = await _applicationDatabaseContext.Regions
                .SingleOrDefaultAsync(region => region.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("regions/{id}")]
        public async Task<IActionResult> DeleteRegion([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete Region : {id}");
            _applicationDatabaseContext.Regions.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
