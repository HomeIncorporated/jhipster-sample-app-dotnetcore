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
    public class JobHistoryController : ControllerBase {
        private const string EntityName = "jobHistory";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<JobHistoryController> _log;

        public JobHistoryController(ILogger<JobHistoryController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("job-histories")]
        [ValidateModel]
        public async Task<ActionResult<JobHistory>> CreateJobHistory([FromBody] JobHistory jobHistory)
        {
            _log.LogDebug($"REST request to save JobHistory : {jobHistory}");
            if (jobHistory.Id != 0)
                throw new BadRequestAlertException("A new jobHistory cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(jobHistory);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetJobHistory), new { id = jobHistory.Id }, jobHistory)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, jobHistory.Id.ToString()));
        }

        [HttpPut("job-histories")]
        [ValidateModel]
        public async Task<IActionResult> UpdateJobHistory([FromBody] JobHistory jobHistory)
        {
            _log.LogDebug($"REST request to update JobHistory : {jobHistory}");
            if (jobHistory.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(jobHistory);
            /* Force the reference navigation property to be in "modified" state.
            This allows to modify it with a null value (the field is nullable).
            This takes into consideration the case of removing the association between the two instances. */
            _applicationDatabaseContext.Entry(jobHistory).Reference(jobHistory0 => jobHistory0.Job).IsModified = true;
            _applicationDatabaseContext.Entry(jobHistory).Reference(jobHistory0 => jobHistory0.Department).IsModified = true;
            _applicationDatabaseContext.Entry(jobHistory).Reference(jobHistory0 => jobHistory0.Employee).IsModified = true;
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(jobHistory)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, jobHistory.Id.ToString()));
        }

        [HttpGet("job-histories")]
        public ActionResult<IEnumerable<JobHistory>> GetAllJobHistories(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of JobHistories");
            var page = _applicationDatabaseContext.JobHistories
                .Include(jobHistory => jobHistory.Job)
                .Include(jobHistory => jobHistory.Department)
                .Include(jobHistory => jobHistory.Employee)
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("job-histories/{id}")]
        public async Task<IActionResult> GetJobHistory([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get JobHistory : {id}");
            var result = await _applicationDatabaseContext.JobHistories
                .Include(jobHistory => jobHistory.Job)
                .Include(jobHistory => jobHistory.Department)
                .Include(jobHistory => jobHistory.Employee)
                .SingleOrDefaultAsync(jobHistory => jobHistory.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("job-histories/{id}")]
        public async Task<IActionResult> DeleteJobHistory([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete JobHistory : {id}");
            _applicationDatabaseContext.JobHistories.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
