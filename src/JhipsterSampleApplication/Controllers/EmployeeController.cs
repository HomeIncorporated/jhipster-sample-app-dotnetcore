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
    public class EmployeeController : ControllerBase {
        private const string EntityName = "employee";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<EmployeeController> _log;

        public EmployeeController(ILogger<EmployeeController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("employees")]
        [ValidateModel]
        public async Task<ActionResult<Employee>> CreateEmployee([FromBody] Employee employee)
        {
            _log.LogDebug($"REST request to save Employee : {employee}");
            if (employee.Id != 0)
                throw new BadRequestAlertException("A new employee cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(employee);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetEmployee), new { id = employee.Id }, employee)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, employee.Id.ToString()));
        }

        [HttpPut("employees")]
        [ValidateModel]
        public async Task<IActionResult> UpdateEmployee([FromBody] Employee employee)
        {
            _log.LogDebug($"REST request to update Employee : {employee}");
            if (employee.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(employee);
            /* Force the reference navigation property to be in "modified" state.
            This allows to modify it with a null value (the field is nullable).
            This takes into consideration the case of removing the association between the two instances. */
            _applicationDatabaseContext.Entry(employee).Reference(employee0 => employee0.Manager).IsModified = true;
            _applicationDatabaseContext.Entry(employee).Reference(employee0 => employee0.Department).IsModified = true;
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(employee)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, employee.Id.ToString()));
        }

        [HttpGet("employees")]
        public ActionResult<IEnumerable<Employee>> GetAllEmployees(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of Employees");
            var page = _applicationDatabaseContext.Employees
                .Include(employee => employee.Manager)
                .Include(employee => employee.Department)
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("employees/{id}")]
        public async Task<IActionResult> GetEmployee([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get Employee : {id}");
            var result = await _applicationDatabaseContext.Employees
                .Include(employee => employee.Manager)
                .Include(employee => employee.Department)
                .SingleOrDefaultAsync(employee => employee.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("employees/{id}")]
        public async Task<IActionResult> DeleteEmployee([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete Employee : {id}");
            _applicationDatabaseContext.Employees.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
