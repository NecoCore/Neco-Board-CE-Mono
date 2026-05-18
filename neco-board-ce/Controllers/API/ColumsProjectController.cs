using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Authorize]
    [Route("api/project/{projectId}/column")]
    public class ColumsProjectController : UserAuth
    {
        private readonly ILogger<ColumsProjectController> _logger;
        private readonly ColumnsRepository _repository;
        private readonly IHubContext<ProjectHub> _projectHubContext;
        private readonly UserAccessCheck _userAccess;
        private readonly UserProjectRoleRepository _userProjectReposirory;

        public ColumsProjectController(ILogger<ColumsProjectController> logger, ColumnsRepository repository, UserProjectRoleRepository userProjectReposirory, IHubContext<ProjectHub> projectHubContext)
        {
            _logger = logger;
            _repository = repository;
            _userAccess = new UserAccessCheck(userProjectReposirory);
            _userProjectReposirory = userProjectReposirory;
            _projectHubContext = projectHubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetColumnsProject(string projectId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId, projectId);
            if(!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _userProjectReposirory.GetByProjectId(projectId);
            if (!result.Success) return BadRequest(new { result.Message });
            if (result.Data is null || result.Data.Count == 0) return NoContent();

            var columns = await _repository.GetByProjectId(projectId);
            return Ok(columns);
        }

        [HttpPost]
        public async Task<IActionResult> CreateColumnProject(string projectId, [FromBody] ColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId, projectId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var result = await _repository.GetByProjectId(projectId);
            if(!result.Success) return BadRequest(result.Message);
            if(result.Data is null) return BadRequest("Project not found");

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
                return Ok();
            }
            return BadRequest(createResult.Message);
        }

        [HttpPut("{columnId}")]
        public async Task<IActionResult> UpdateColumn(string columnId, string projectId, [FromBody] ColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId, projectId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var updateColumn = new Column
            {
                Name = dto.Name
            };
            var updateResult = await _repository.Update(columnId, updateColumn);

            if (updateResult.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_UPDATED);
                return Ok();
            }
            return BadRequest(updateResult.Message);
        }

        [HttpPut("{columnId}/order")]
        public async Task<IActionResult> UpdateColumnOrder(string columnId, string projectId, [FromBody] int queue)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId, projectId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var result = await _repository.UpdateOrder(projectId, columnId, queue);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_UPDATED_ORDER);
                return Ok();
            }
            return BadRequest(result.Message);
        }

        [HttpDelete("{columnId}")]
        public async Task<IActionResult> DeleteColumn(string columnId, string projectId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId, projectId, ProjectRole.MODERATOR);
            if (!IsWorkspaceAdmin() && !accessResult.Result) return Forbid();

            var result = await _repository.Delete(columnId);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_DELETED);
                return Ok();
            }
            return BadRequest(result.Message);
        }
    }
}
