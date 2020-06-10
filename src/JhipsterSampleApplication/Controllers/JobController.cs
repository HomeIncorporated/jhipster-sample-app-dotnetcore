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
    public class JobController : ControllerBase {
        private const string EntityName = "job";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<JobController> _log;

        public JobController(ILogger<JobController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("jobs")]
        [ValidateModel]
        public async Task<ActionResult<Job>> CreateJob([FromBody] Job job)
        {
            _log.LogDebug($"REST request to save Job : {job}");
            if (job.Id != 0)
                throw new BadRequestAlertException("A new job cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(job);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, job.Id.ToString()));
        }

        [HttpPut("jobs")]
        [ValidateModel]
        public async Task<IActionResult> UpdateJob([FromBody] Job job)
        {
            _log.LogDebug($"REST request to update Job : {job}");
            if (job.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.JobChores.RemoveNavigationProperty(job, job.Id);
            _applicationDatabaseContext.Update(job);
            /* Force the reference navigation property to be in "modified" state.
            This allows to modify it with a null value (the field is nullable).
            This takes into consideration the case of removing the association between the two instances. */
            _applicationDatabaseContext.Entry(job).Reference(job0 => job0.Employee).IsModified = true;
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(job)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, job.Id.ToString()));
        }

        [HttpGet("jobs")]
        public ActionResult<IEnumerable<Job>> GetAllJobs(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of Jobs");
            var page = _applicationDatabaseContext.Jobs
                .Include(job => job.Employee)
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("jobs/{id}")]
        public async Task<IActionResult> GetJob([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get Job : {id}");
            var result = await _applicationDatabaseContext.Jobs
                .Include(job => job.JobChores)
                    .ThenInclude(jobChore => jobChore.PieceOfWork)
                .Include(job => job.Employee)
                .SingleOrDefaultAsync(job => job.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("jobs/{id}")]
        public async Task<IActionResult> DeleteJob([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete Job : {id}");
            _applicationDatabaseContext.Jobs.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
