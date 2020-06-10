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
    public class DepartmentController : ControllerBase {
        private const string EntityName = "department";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<DepartmentController> _log;

        public DepartmentController(ILogger<DepartmentController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("departments")]
        [ValidateModel]
        public async Task<ActionResult<Department>> CreateDepartment([FromBody] Department department)
        {
            _log.LogDebug($"REST request to save Department : {department}");
            if (department.Id != 0)
                throw new BadRequestAlertException("A new department cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(department);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetDepartment), new { id = department.Id }, department)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, department.Id.ToString()));
        }

        [HttpPut("departments")]
        [ValidateModel]
        public async Task<IActionResult> UpdateDepartment([FromBody] Department department)
        {
            _log.LogDebug($"REST request to update Department : {department}");
            if (department.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(department);
            /* Force the reference navigation property to be in "modified" state.
            This allows to modify it with a null value (the field is nullable).
            This takes into consideration the case of removing the association between the two instances. */
            _applicationDatabaseContext.Entry(department).Reference(department0 => department0.Location).IsModified = true;
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(department)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, department.Id.ToString()));
        }

        [HttpGet("departments")]
        public ActionResult<IEnumerable<Department>> GetAllDepartments(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of Departments");
            var page = _applicationDatabaseContext.Departments
                .Include(department => department.Location)
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("departments/{id}")]
        public async Task<IActionResult> GetDepartment([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get Department : {id}");
            var result = await _applicationDatabaseContext.Departments
                .Include(department => department.Location)
                .SingleOrDefaultAsync(department => department.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("departments/{id}")]
        public async Task<IActionResult> DeleteDepartment([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete Department : {id}");
            _applicationDatabaseContext.Departments.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
