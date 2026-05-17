using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
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
        private readonly UserProjectRoleRepository _userProjectReposirory;
        private readonly IHubContext<ProjectHub> _projectHubContext;

        public ColumsProjectController(ILogger<ColumsProjectController> logger, ColumnsRepository repository, UserProjectRoleRepository userProjectReposirory, IHubContext<ProjectHub> projectHubContext)
        {
            _logger = logger;
            _repository = repository;
            _userProjectReposirory = userProjectReposirory;
            _projectHubContext = projectHubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetColumnsProject(string projectId)
        {
            var users = await _userProjectReposirory.GetByProjectId(projectId);
            var existing = users.Select(u => u.UserId).Contains(UserId);
            if (!existing && !IsWorkspaceAdmin()) return Forbid();

            var columns = await _repository.GetByProjectId(projectId);
            return Ok(columns);
        }

        [HttpPost]
        public async Task<IActionResult> CreateColumnProject(string projectId, [FromBody] ColumnRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();

                if (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR)
                    return Forbid();
            }

            var columns = await _repository.GetByProjectId(projectId);
            var queue = columns.Count > 0 ? columns.Max(c => c.Queue) + 1 : 1;

            var newColumn = new Column {
                Name = dto.Name,
                Queue = queue,
                ProjectId = projectId,
            };

            var result = await _repository.Create(newColumn);
            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_CREATED);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPut("{columnId}")]
        public async Task<IActionResult> UpdateColumn(string columnId, string projectId, [FromBody] ColumnRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();

                if (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR)
                    return Forbid();
            }

            var updateColumn = new Column
            {
                Name = dto.Name
            };
            var result = await _repository.Update(columnId, updateColumn);

            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_UPDATED);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPut("{columnId}/order")]
        public async Task<IActionResult> UpdateColumnOrder(string columnId, string projectId, [FromBody] int queue)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();

                if (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR)
                    return Forbid();
            }

            var result = await _repository.UpdateOrder(projectId, columnId, queue);
            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_UPDATED_ORDER);
                return Ok();
            }
            return BadRequest();
        }

        [HttpDelete("{columnId}")]
        public async Task<IActionResult> DeleteColumn(string columnId, string projectId)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();

                if (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR)
                    return Forbid();
            }

            var result = await _repository.Delete(columnId);
            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_COLUMN_DELETED);
                return Ok();
            }
            return BadRequest();
        }
    }
}
