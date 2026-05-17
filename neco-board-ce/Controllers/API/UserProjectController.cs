using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Authorize]
    [Route("api/project/{projectId}/users")]
    public class UserProjectController : UserAuth
    {
        private readonly ILogger<UserProjectController> _logger;
        private readonly UserProjectRoleRepository _repository;
        private readonly IHubContext<ProjectHub> _projectHubContext;

        public UserProjectController(ILogger<UserProjectController> logger, UserProjectRoleRepository repository, IHubContext<ProjectHub> projectHubContext)
        {
            _logger = logger;
            _repository = repository;
            _projectHubContext = projectHubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetUsersProjectById(string projectId)
        {
            var users = await _repository.GetByProjectId(projectId);
            if (users == null || !users.Any()) return NotFound();

            if (!IsWorkspaceAdmin())
            {
                var userId = UserId; 
                var isMember = users.Any(u => u.UserId == userId);
                if (!isMember) return Forbid();
            }

            var usersInfo = users.Select(x => new
            {
                id = x.UserId,
                name = x.User.Name,
                avatar = x.User.Avatar,
                role = x.Role,
            });

            return Ok(usersInfo);
        }

        [HttpPost]
        public async Task<IActionResult> AddUserInProject([FromBody] UserProjectRequest dto, string projectId)
        {
            if (UserId is null) return Unauthorized();

            if(!IsWorkspaceAdmin())
            {
                var user = await _repository.GetByUserAndProject(UserId, projectId);
                if (user is null || (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR))
                    return Forbid();
            }

            var result = await _repository.AddToProject(dto.Id, projectId, dto.Role);
            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_USER_ADDED_TO_PROJECT);
                await _projectHubContext.Clients.User(dto.Id).SendAsync(Constants.SOKET_EVENT_PROJECT_CREATED);
                return Ok();
            }
            return NotFound();
        }


        [HttpPatch("{userId}")]
        [Authorize]
        public async Task<IActionResult> UpdateUserInProject(string projectId, string userId, [FromBody] EditUserInProjectRequest dto)
        {
            if (UserId is null) return Unauthorized();

            if (!IsWorkspaceAdmin())
            {
                var user = await _repository.GetByUserAndProject(UserId, projectId);

                if (user is null || (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR))
                    return Forbid();

                var editUser = await _repository.GetByUserAndProject(userId, projectId);
                if (editUser is null) return NotFound();

                if (user.Role >= editUser.Role || user.Role >= dto.Role)
                    return Forbid();
            }
            else
            {
                var editUser = await _repository.GetByUserAndProject(userId, projectId);
                if (editUser is null) return NotFound();
            }

            var result = await _repository.UpdateRole(userId, projectId, dto.Role);
            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_USER_ROLE_UPDATED_IN_PROJECT, userId);
                await _projectHubContext.Clients.User(userId).SendAsync(Constants.SOKET_EVENT_USER_ROLE_UPDATED_IN_PROJECT, userId);
                return Ok();
            }
            return NotFound();
        }

        [HttpDelete("{userId}")]
        [Authorize]
        public async Task<IActionResult> DeleteUserInProject(string projectId, string userId)
        {
            if (UserId is null) return Unauthorized();

            if (!IsWorkspaceAdmin())
            {
                var user = await _repository.GetByUserAndProject(UserId, projectId);

                if (user is null || (user.Role != ProjectRole.OWNER && user.Role != ProjectRole.MODERATOR))
                    return Forbid();

                var editUser = await _repository.GetByUserAndProject(userId, projectId);
                if (editUser is null) return NotFound();

                if (user.Role >= editUser.Role)
                    return Forbid();
            }
            else
            {
                var editUser = await _repository.GetByUserAndProject(userId, projectId);
                if (editUser is null) return NotFound();
            }

            var result = await _repository.RemoveFromProject(userId, projectId);
            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_USER_REMOVED_FROM_PROJECT, userId);
                await _projectHubContext.Clients.User(userId).SendAsync(Constants.SOKET_EVENT_USER_REMOVED_FROM_PROJECT, userId);
                return Ok();
            }
            return NotFound();
        }
    }
}
