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
    public class CountryController : ControllerBase {
        private const string EntityName = "country";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<CountryController> _log;

        public CountryController(ILogger<CountryController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("countries")]
        [ValidateModel]
        public async Task<ActionResult<Country>> CreateCountry([FromBody] Country country)
        {
            _log.LogDebug($"REST request to save Country : {country}");
            if (country.Id != 0)
                throw new BadRequestAlertException("A new country cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(country);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetCountry), new { id = country.Id }, country)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, country.Id.ToString()));
        }

        [HttpPut("countries")]
        [ValidateModel]
        public async Task<IActionResult> UpdateCountry([FromBody] Country country)
        {
            _log.LogDebug($"REST request to update Country : {country}");
            if (country.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(country);
            /* Force the reference navigation property to be in "modified" state.
            This allows to modify it with a null value (the field is nullable).
            This takes into consideration the case of removing the association between the two instances. */
            _applicationDatabaseContext.Entry(country).Reference(country0 => country0.Region).IsModified = true;
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(country)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, country.Id.ToString()));
        }

        [HttpGet("countries")]
        public ActionResult<IEnumerable<Country>> GetAllCountries(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of Countries");
            var page = _applicationDatabaseContext.Countries
                .Include(country => country.Region)
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("countries/{id}")]
        public async Task<IActionResult> GetCountry([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get Country : {id}");
            var result = await _applicationDatabaseContext.Countries
                .Include(country => country.Region)
                .SingleOrDefaultAsync(country => country.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("countries/{id}")]
        public async Task<IActionResult> DeleteCountry([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete Country : {id}");
            _applicationDatabaseContext.Countries.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
