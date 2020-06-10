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
    public class PieceOfWorkController : ControllerBase {
        private const string EntityName = "pieceOfWork";

        private readonly ApplicationDatabaseContext _applicationDatabaseContext;

        private readonly ILogger<PieceOfWorkController> _log;

        public PieceOfWorkController(ILogger<PieceOfWorkController> log,
            ApplicationDatabaseContext applicationDatabaseContext)
        {
            _log = log;
            _applicationDatabaseContext = applicationDatabaseContext;
        }

        [HttpPost("piece-of-works")]
        [ValidateModel]
        public async Task<ActionResult<PieceOfWork>> CreatePieceOfWork([FromBody] PieceOfWork pieceOfWork)
        {
            _log.LogDebug($"REST request to save PieceOfWork : {pieceOfWork}");
            if (pieceOfWork.Id != 0)
                throw new BadRequestAlertException("A new pieceOfWork cannot already have an ID", EntityName, "idexists");
            _applicationDatabaseContext.AddGraph(pieceOfWork);
            await _applicationDatabaseContext.SaveChangesAsync();
            return CreatedAtAction(nameof(GetPieceOfWork), new { id = pieceOfWork.Id }, pieceOfWork)
                .WithHeaders(HeaderUtil.CreateEntityCreationAlert(EntityName, pieceOfWork.Id.ToString()));
        }

        [HttpPut("piece-of-works")]
        [ValidateModel]
        public async Task<IActionResult> UpdatePieceOfWork([FromBody] PieceOfWork pieceOfWork)
        {
            _log.LogDebug($"REST request to update PieceOfWork : {pieceOfWork}");
            if (pieceOfWork.Id == 0) throw new BadRequestAlertException("Invalid Id", EntityName, "idnull");
            //TODO catch //DbUpdateConcurrencyException into problem
            _applicationDatabaseContext.Update(pieceOfWork);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok(pieceOfWork)
                .WithHeaders(HeaderUtil.CreateEntityUpdateAlert(EntityName, pieceOfWork.Id.ToString()));
        }

        [HttpGet("piece-of-works")]
        public ActionResult<IEnumerable<PieceOfWork>> GetAllPieceOfWorks(IPageable pageable)
        {
            _log.LogDebug("REST request to get a page of PieceOfWorks");
            var page = _applicationDatabaseContext.PieceOfWorks
                .UsePageable(pageable);
            return Ok(page.Content).WithHeaders(page.GeneratePaginationHttpHeaders());
        }

        [HttpGet("piece-of-works/{id}")]
        public async Task<IActionResult> GetPieceOfWork([FromRoute] long id)
        {
            _log.LogDebug($"REST request to get PieceOfWork : {id}");
            var result = await _applicationDatabaseContext.PieceOfWorks
                .SingleOrDefaultAsync(pieceOfWork => pieceOfWork.Id == id);
            return ActionResultUtil.WrapOrNotFound(result);
        }

        [HttpDelete("piece-of-works/{id}")]
        public async Task<IActionResult> DeletePieceOfWork([FromRoute] long id)
        {
            _log.LogDebug($"REST request to delete PieceOfWork : {id}");
            _applicationDatabaseContext.PieceOfWorks.RemoveById(id);
            await _applicationDatabaseContext.SaveChangesAsync();
            return Ok().WithHeaders(HeaderUtil.CreateEntityDeletionAlert(EntityName, id.ToString()));
        }
    }
}
