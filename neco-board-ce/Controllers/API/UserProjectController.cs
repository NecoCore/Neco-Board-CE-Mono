using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Attributes.ProjectAccessAttribute;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Request.Projects;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.DTO.Response.Users;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Services.Logs;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Provides endpoints for managing project membership: listing members, adding, updating roles, and removing users.
    /// </summary>
    /// <remarks>
    /// All endpoints are scoped to a single project identified by the <c>{projectId}</c> route parameter.
    /// Write operations require at least the <c>MODERATOR</c> role in the project,
    /// or workspace administrator privileges.
    /// Role hierarchy rules prevent moderators from modifying users of equal or higher rank.
    /// Successful mutations broadcast real-time SignalR events to the project group
    /// and to the affected user's personal connection.
    /// </remarks>
    [ApiController]
    [Authorize]
    [Route("api/project/{projectId:guid}/users")]
    [Tags("Project members")]
    public class UserProjectController : UserAuth
    {
        private readonly ILogger<UserProjectController> _logger;
        private readonly UserProjectRoleRepository _repository;
        private readonly AuditService _auditService;
        private readonly IRealtimeNotifier _notifier;

        public UserProjectController(
            ILogger<UserProjectController> logger,
            UserProjectRoleRepository repository,
            AuditService auditService,
            IRealtimeNotifier notifier
        ) {
            _logger = logger;
            _repository = repository;
            _auditService = auditService;
            _notifier = notifier;
        }

        /// <summary>
        /// Get All Users In Project
        /// </summary>
        /// <remarks>
        /// Returns the list of all members in the specified project, including their roles.
        /// Access requires any project membership or workspace administrator privileges.
        /// Returns <c>204 No Content</c> when the repository succeeds but the project has no members.
        /// Each item is mapped to a <see cref="UserInfoProjectResponse"/> that includes the user's project role.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project (route parameter).</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="UserInfoProjectResponse"/> on success;
        /// <see cref="NoContentResult"/> when the project has no members;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller is not a project member and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the list of project members with their roles.</response>
        /// <response code="204">The project has no members.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not a project member and is not a workspace administrator.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet(Name = "GetAllUsersInProject")]
        [ProjectAccess]
        [ProducesResponseType(typeof(List<UserInfoProjectResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUsersProjectById(Guid projectId)
        {
            var result = await _repository.GetByProjectId(projectId);
            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve members for project '{ProjectId}': {Error}", projectId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the project member list. Please try again later." });
            }
            if (result.Data is null || result.Data.Count == 0) return NoContent();

            var data = result.Data.Select(x => new UserInfoProjectResponse(x)).ToList();
            return Ok(data);
        }

        /// <summary>
        /// Add User In Project
        /// </summary>
        /// <remarks>
        /// Adds a user to the project with the specified role.
        /// Requires at least the <c>MODERATOR</c> role in the project, or workspace administrator privileges.
        /// On success, two SignalR events are broadcast:
        /// <list type="bullet">
        ///   <item><description><c>SOCKET_EVENT_USER_ADDED_TO_PROJECT</c> — sent to the project group.</description></item>
        ///   <item><description><c>SOCKET_EVENT_PROJECT_CREATED</c> — sent to the added user's personal connection to trigger a project list refresh.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="dto">Request body containing the target user ID and the role to assign.</param>
        /// <param name="projectId">The unique identifier of the project (route parameter).</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks MODERATOR role and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">User added to the project successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have MODERATOR role and is not a workspace administrator.</response>
        /// <response code="500">Failed to add the user to the project. Response body contains the error description.</response>
        [HttpPost(Name = "AddUserInProject")]
        [ProjectAccess(ProjectRole.USER)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> AddUserInProject([FromBody] UserProjectRequest dto, Guid projectId)
        {
            if (!IsWorkspaceAdmin())
            {
                var rolesResult = await _repository.GetByUserAndProject(UserId!.Value, projectId);
                if (!rolesResult.Success || rolesResult.Data is null) return NotFound();

                // Privilege Escalation check: cannot assign a role higher than your own
                if (dto.Role < rolesResult.Data.Role)
                    return Forbid();
            }

            var result = await _repository.AddToProject(dto.Id, projectId, dto.Role);
            if (result.Success)
            {
                await _auditService.ProjectLog(projectId, UserId!.Value, LogType.EDITED, "User added to project", $"Added UserId: {dto.Id}, Role: {dto.Role}");
                await _notifier.ProjectAddUser(projectId, dto.Id);
                return NoContent();
            }

            _logger.LogError("Failed to add user '{TargetUserId}' to project '{ProjectId}': {Error}", dto.Id, projectId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to add the user to the project. Please try again later." });
        }

        /// <summary>
        /// Update User In Project
        /// </summary>
        /// <remarks>
        /// Updates the project role of a member.
        /// Requires at least the <c>MODERATOR</c> role in the project, or workspace administrator privileges.
        /// When the caller is not a workspace administrator, the following additional role hierarchy rules apply:
        /// <list type="bullet">
        ///   <item><description>The target user's role is <c>OWNER</c> — returns <c>403</c>.</description></item>
        ///   <item><description>The target user's role is <c>MODERATOR</c> and the caller's role is <c>MODERATOR</c> or higher — returns <c>403</c>.</description></item>
        ///   <item><description>The target user's role equals the caller's role — returns <c>403</c>.</description></item>
        /// </list>
        /// Returns <c>404</c> when the target user or the calling user is not a member of the project.
        /// On success, broadcasts <c>SOCKET_EVENT_USER_ROLE_UPDATED_IN_PROJECT</c> to both
        /// the project group and the affected user's personal connection.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project (route parameter).</param>
        /// <param name="userId">The unique identifier of the member whose role is to be updated (route parameter).</param>
        /// <param name="dto">Request body containing the new project role.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks permission or role hierarchy rules are violated;
        /// <see cref="NotFoundResult"/> when the target user or the caller is not a project member;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">Project role updated successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller lacks permission or role hierarchy rules prevent this update.</response>
        /// <response code="404">The target user or the calling user is not a member of the project.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpPatch("{userId:guid}", Name = "UpdateUserInProject")]
        [Authorize]
        [ProjectAccess(ProjectRole.USER)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateUserInProject(Guid projectId, Guid userId, [FromBody] EditUserInProjectRequest dto)
        {
            var result = await _repository.GetByUserAndProject(userId, projectId);
            if(!result.Success)
            {
                _logger.LogError("Failed to retrieve membership record for user '{TargetUserId}' in project '{ProjectId}': {Error}", userId, projectId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user's project membership. Please try again later." });
            }
            if (result.Data is null) return NotFound();

            if(!IsWorkspaceAdmin())
            {
                var rolesResult = await _repository.GetByUserAndProject(UserId!.Value, projectId);
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
                await _auditService.ProjectLog(projectId, UserId!.Value, LogType.EDITED, "Project member role updated", $"UserId: {userId}, New Role: {dto.Role}");
                await _notifier.ProjectUpdateUser(projectId, userId, dto.Role);
                return NoContent();
            }

            _logger.LogError("Failed to update project role for user '{TargetUserId}' in project '{ProjectId}': {Error}", userId, projectId, editResult.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to update the user's project role. Please try again later." });
        }

        /// <summary>
        /// Remove User From Project
        /// </summary>
        /// <remarks>
        /// Removes a user from the project.
        /// Requires at least the <c>MODERATOR</c> role in the project, or workspace administrator privileges.
        /// When the caller is not a workspace administrator, the same role hierarchy rules as
        /// <c>UpdateUserInProject</c> apply — an <c>OWNER</c> cannot be removed, and a moderator
        /// cannot remove a peer or a higher-ranked member.
        /// Returns <c>404</c> when the target user or the calling user is not a member of the project.
        /// On success, broadcasts <c>SOCKET_EVENT_USER_REMOVED_FROM_PROJECT</c> to both
        /// the project group and the removed user's personal connection.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project (route parameter).</param>
        /// <param name="userId">The unique identifier of the member to remove (route parameter).</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks permission or role hierarchy rules are violated;
        /// <see cref="NotFoundResult"/> when the target user or the caller is not a project member;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">User removed from the project successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller lacks permission or role hierarchy rules prevent this removal.</response>
        /// <response code="404">The target user or the calling user is not a member of the project.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpDelete("{userId:guid}", Name = "RemoveUserFromProject")]
        [Authorize]
        [ProjectAccess(ProjectRole.USER)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUserInProject(Guid projectId, Guid userId)
        {
            var result = await _repository.GetByUserAndProject(userId, projectId);
            if (!result.Success)
            {
                _logger.LogError("Failed to retrieve membership record for user '{TargetUserId}' in project '{ProjectId}': {Error}", userId, projectId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user's project membership. Please try again later." });
            }
            if (result.Data is null) return NotFound();

            if (!IsWorkspaceAdmin())
            {
                var rolesResult = await _repository.GetByUserAndProject(UserId!.Value, projectId);
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
                await _auditService.ProjectLog(projectId, UserId!.Value, LogType.EDITED, "User removed from project", $"Removed UserId: {userId}");
                await _notifier.ProjectRemoveUser(projectId, userId);
                return NoContent();
            }

            _logger.LogError("Failed to remove user '{TargetUserId}' from project '{ProjectId}': {Error}", userId, projectId, removingResult.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to remove the user from the project. Please try again later." });
        }
    }
}
