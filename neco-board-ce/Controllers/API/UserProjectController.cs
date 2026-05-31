using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.DTO.Response.Massages;
using neco_board_ce.Models.DTO.Response.Users;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
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
        private readonly UserAccessCheck _userAccess;

        public UserProjectController(
            ILogger<UserProjectController> logger, 
            UserProjectRoleRepository repository, 
            IHubContext<ProjectHub> projectHubContext,
            UserAccessCheck userAccess
        ) {
            _logger = logger;
            _repository = repository;
            _projectHubContext = projectHubContext;
            _userAccess = userAccess;
        }

        [HttpGet(Name = "GetAllUsersInProject")]
        [ProducesResponseType(typeof(List<UserInfoProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsersProjectById(string projectId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId);
            if(!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByProjectId(projectId);
            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve all users in project {ProjectId}: {Error}", projectId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
            }
            if (result.Data is null) return NoContent();

            var data = result.Data.Select(x => new UserInfoProjectResponse(x)).ToList();
            return Ok(data);
        }

        [HttpPost(Name = "AddUserInProject")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddUserInProject([FromBody] UserProjectRequest dto, string projectId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId, ProjectRole.MODERATOR);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.AddToProject(dto.Id, projectId, dto.Role);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_USER_ADDED_TO_PROJECT);
                await _projectHubContext.Clients.User(dto.Id).SendAsync(Constants.SOKET_EVENT_PROJECT_CREATED);
                return NoContent();
            }

            _logger.LogError("Failed to retrieve all users in project {ProjectId}: {Error}", projectId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
        }


        [HttpPatch("{userId}", Name = "UpdateUserInProject")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserInProject(string projectId, string userId, [FromBody] EditUserInProjectRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId, ProjectRole.MODERATOR);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByUserAndProject(userId, projectId);
            if(!result.Success)
            {
                _logger.LogError("Failed to retrieve all users in project {ProjectId}: {Error}", projectId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
            }
            if (result.Data is null) return NotFound();

            if(!IsWorkspaceAdmin())
            {
                var rolesResult = await _repository.GetByUserAndProject(UserId!, projectId);
                if(!rolesResult.Success || rolesResult.Data is null) return NotFound();

                if (result.Data.Role == ProjectRole.OWNER)
                    return Forbid();
                else if (result.Data.Role == ProjectRole.MODERATOR && rolesResult.Data.Role >= ProjectRole.MODERATOR)
                    return Forbid();
                else if (result.Data.Role == rolesResult.Data.Role)
                    return Forbid();
            }

            var editResult = await _repository.UpdateRole(userId, projectId, dto.Role);
            if (editResult.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_USER_ROLE_UPDATED_IN_PROJECT, userId);
                await _projectHubContext.Clients.User(userId).SendAsync(Constants.SOKET_EVENT_USER_ROLE_UPDATED_IN_PROJECT, userId);
                return NoContent();
            }

            _logger.LogError("Failed to retrieve all users in project {ProjectId}: {Error}", projectId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
        }

        [HttpDelete("{userId}", Name = "RemoveUserFromProject")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUserInProject(string projectId, string userId)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, projectId, ProjectRole.MODERATOR);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByUserAndProject(userId, projectId);
            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve all users in project {ProjectId}: {Error}", projectId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
            }
            if (result.Data is null) return NotFound();

            if (!IsWorkspaceAdmin())
            {
                var rolesResult = await _repository.GetByUserAndProject(UserId!, projectId);
                if (!rolesResult.Success || rolesResult.Data is null) return NotFound();

                if (result.Data.Role == ProjectRole.OWNER)
                    return Forbid();
                else if (result.Data.Role == ProjectRole.MODERATOR && rolesResult.Data.Role >= ProjectRole.MODERATOR)
                    return Forbid();
                else if (result.Data.Role == rolesResult.Data.Role)
                    return Forbid();
            }

            var removingResult = await _repository.RemoveFromProject(userId, projectId);
            if (removingResult.Success)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_USER_REMOVED_FROM_PROJECT, userId);
                await _projectHubContext.Clients.User(userId).SendAsync(Constants.SOKET_EVENT_USER_REMOVED_FROM_PROJECT, userId);
                return NoContent();
            }

            _logger.LogError("Failed to retrieve all users in project {ProjectId}: {Error}", projectId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
        }
    }
}
