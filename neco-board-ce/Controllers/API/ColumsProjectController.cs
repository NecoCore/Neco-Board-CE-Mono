using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.DTO.Response.Column;
using neco_board_ce.Models.DTO.Response.Massages;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Authorize]
    [Route("api/column")]
    public class ColumsProjectController : UserAuth
    {
        private readonly ILogger<ColumsProjectController> _logger;
        private readonly ColumnsRepository _repository;
        private readonly IHubContext<ProjectHub> _projectHubContext;
        private readonly UserAccessCheck _userAccess;

        public ColumsProjectController(
            ILogger<ColumsProjectController> logger, 
            ColumnsRepository repository,
            IHubContext<ProjectHub> projectHubContext,
            UserAccessCheck userAccess
            )
        {
            _logger = logger;
            _repository = repository;
            _userAccess = userAccess;
            _projectHubContext = projectHubContext;
        }

        [HttpGet("in-project/{projectId}", Name = "GetColumnsInProject")]
        [ProducesResponseType(typeof(List<ColumnItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetColumnsProject(string projectId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId);
            if(!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByProjectId(projectId);
            if(result.Success)
            {
                var data = result.Data?.Select(c => new ColumnItemResponse(c)).ToList();
                return data is null ? NoContent() : Ok(data);
            }
            _logger.LogError("Failed to get columns in project {projectId}: {error}", projectId, result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the column list. Please try again later." });
        }

        [HttpPost("in-project/{projectId}", Name = "CreateColumnInProject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateColumnProject(string projectId, [FromBody] ColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var result = await _repository.GetByProjectId(projectId);
            if(!result.Success) return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "unknown error" });
            if(result.Data is null) return NotFound(new ErrorMessageResponse { Message = result.Message ?? "unknown error" });

            var columns = result.Data;
            var queue = columns.Count > 0 ? columns.Max(c => c.Queue) + 1 : 1;

            var newColumn = new Column {
                Name = dto.Name,
                Queue = queue,
                ProjectId = projectId,
            };

            var createResult = await _repository.Create(newColumn);
            if (createResult.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_CREATED);
                return NoContent();
            }
            _logger.LogError("Failed to create column in project {projectId}: {error}", projectId, result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve for create the column. Please try again later." });
        }

        [HttpPut("{columnId}", Name = "UpdateColumn")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateColumn(string columnId, [FromBody] ColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToColumn(UserId!, columnId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var updateColumn = new Column
            {
                Name = dto.Name
            };
            var updateResult = await _repository.Update(columnId, updateColumn);

            if (updateResult.Success)
            {
                await _projectHubContext.Clients.Group(accessResult.ProjectId!).SendAsync(Constants.SOKET_EVENT_COLUMN_UPDATED);
                return NoContent();
            }

            _logger.LogError("Failed to update column {columnId}: {error}", columnId, updateResult.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve to update the column. Please try again later." });
        }

        [HttpPut("{columnId}/order", Name = "UpdateColumnOrder")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateColumnOrder(string columnId, [FromBody] int queue)
        {
            var accessResult = await _userAccess.HasAccessToColumn(UserId!, columnId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var result = await _repository.UpdateOrder(accessResult.ProjectId!, columnId, queue);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(accessResult.ProjectId!).SendAsync(Constants.SOKET_EVENT_COLUMN_UPDATED_ORDER);
                return Ok();
            }

            _logger.LogError("Failed to update order column {columnId}: {error}", columnId, result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve to update order the column. Please try again later." });
        }

        [HttpDelete("{columnId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteColumn(string columnId, string projectId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var result = await _repository.Delete(columnId);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_DELETED);
                return NoContent();
            }

            _logger.LogError("Failed to delete the column {columnId}: {error}", columnId, result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve to delete the column. Please try again later." });
        }
    }
}
