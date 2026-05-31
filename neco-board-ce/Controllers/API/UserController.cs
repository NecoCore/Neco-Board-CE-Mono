using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.DTO.Response.Massages;
using neco_board_ce.Models.DTO.Response.Projects;
using neco_board_ce.Models.DTO.Response.Users;
using neco_board_ce.Models.Entity;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Provides endpoints for user management: listing, role assignment, password change,
    /// retrieving personal projects and tasks, and account deletion.
    /// </summary>
    /// <remarks>
    /// All endpoints require authentication via the class-level <c>[Authorize]</c> attribute.
    /// Some operations are further restricted by workspace role (<c>ADMIN</c>, <c>OWNER</c>).
    /// Repository failures return <c>500 Internal Server Error</c> to distinguish infrastructure
    /// problems from invalid client input.
    /// </remarks>
    [ApiController]
    [Route("api/users")]
    [Authorize]
    [Tags("Users")]
    public class UserController : UserAuth
    {
        private readonly ILogger<UserController> _logger;
        private readonly AccountRepository _repository;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskUserRepository _taskUserRepository;

        public UserController(
            ILogger<UserController> logger,
            AccountRepository repository,
            ProjectRepository projectRepository,
            TaskUserRepository taskUserRepository
            )
        {
            _logger = logger;
            _repository = repository;
            _projectRepository = projectRepository;
            _taskUserRepository = taskUserRepository;
        }

        /// <summary>
        /// Get All Users
        /// </summary>
        /// <remarks>
        /// Returns a public summary list of all registered users.
        /// Each item is mapped to a <see cref="UserInfoResponse"/> that contains only
        /// publicly visible fields. Use <c>GET /api/users/all</c> for the full account
        /// data (requires <c>ADMIN</c> or <c>OWNER</c> role).
        /// Returns <c>204 No Content</c> when the repository succeeds but no users exist.
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="UserInfoResponse"/> on success;
        /// <see cref="NoContentResult"/> when no users exist;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the public summary list of all users.</response>
        /// <response code="204">No users are registered in the workspace.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet(Name = "GetAllUsers")]
        [ProducesResponseType(typeof(List<UserInfoResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll()
        {
            var resut = await _repository.GetAll();
            if (!resut.Success)
            {
                _logger.LogError("Failed to retrieve all users: {Error}", resut.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
            }
            else if (resut.Data is null) return NoContent();

            var data = resut.Data.Select(x => new UserInfoResponse(x)).ToList();
            return Ok(data);
        }

        /// <summary>
        /// Get All Users For Admins
        /// </summary>
        /// <remarks>
        /// Returns the full account list with all fields. Restricted to administrators and owners.
        /// Returns the raw <see cref="Account"/> entities without field filtering.
        /// Intended for administrative use only.
        /// Returns <c>204 No Content</c> when the repository succeeds but no users exist.
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="Account"/> on success;
        /// <see cref="NoContentResult"/> when no users exist;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks the ADMIN or OWNER role;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the full account list.</response>
        /// <response code="204">No users are registered in the workspace.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not hold the ADMIN or OWNER role.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet("all", Name = "GetAllUsersForAdmins")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(typeof(List<Account>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllFull()
        {
            var resut = await _repository.GetAll();
            if(!resut.Success)
            {
                _logger.LogError("Failed to retrieve full user list for admin: {Error}", resut.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve the user list. Please try again later." });
            }
            else if (resut.Data is null) return NoContent();
            return Ok(resut.Data);
        }

        /// <summary>
        /// Edit User Role
        /// </summary>
        /// <remarks>
        /// Updates the workspace role of a user. Restricted to the workspace owner.
        /// Only the workspace <c>OWNER</c> may call this endpoint.
        /// Assigning the <c>OWNER</c> role to another user is explicitly prohibited and
        /// returns <c>403 Forbidden</c> immediately without reaching the repository.
        /// </remarks>
        /// <param name="id">The unique identifier of the user whose role is to be changed.</param>
        /// <param name="dto">Request body containing the new workspace role.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="BadRequestObjectResult"/> with <see cref="ErrorMessageResponse"/> when the repository rejects the update;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller is not the OWNER or when the target role is OWNER.
        /// </returns>
        /// <response code="204">User role updated successfully.</response>
        /// <response code="400">Repository rejected the role update. Response body contains the error description.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not the workspace OWNER, or the requested target role is OWNER.</response>
        [HttpPatch("role/{id}", Name = "EditUserRole")]
        [Authorize(Roles = "OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> EditRole(string id, [FromBody] EditWorkspaceRoleRequest dto)
        {
            if (dto.Role == Models.Enums.WorkspaceRoles.OWNER)
                return Forbid();

            var result = await _repository.UpdateRole(id, dto.Role);
            if(!result.Success)
            {
                _logger.LogWarning("Failed to update role for user '{UserId}': {Error}", id, result.Message ?? "unknown error");
                return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Failed to update the user role." });
            }

            return NoContent();
        }

        /// <summary>
        /// Update User Password
        /// </summary>
        /// <remarks>
        /// Changes the password of the currently authenticated user.
        /// The caller must supply the current password (<c>OldPassword</c>) for verification.
        /// The new password (<c>Password</c>) must match the confirmation field (<c>ConfirmPassword</c>).
        /// Both validation failures return <c>400 Bad Request</c> with a descriptive message.
        /// The endpoint operates on the caller's own account — the user ID is taken from the JWT,
        /// not from the request body.
        /// </remarks>
        /// <param name="dto">Request body containing the current password, new password, and confirmation.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="BadRequestObjectResult"/> with <see cref="ErrorMessageResponse"/> when validation fails;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="NotFoundObjectResult"/> with <see cref="ErrorMessageResponse"/> when the caller's account is not found;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">Password changed successfully.</response>
        /// <response code="400">Current password is incorrect, or the new passwords do not match.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="404">The authenticated user's account was not found.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpPatch("updatePassword", Name = "UpdateUserPassword")]
        [Authorize]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> EditPassword(EditPasswordRequest dto)
        {
            var result = await _repository.GetById(UserId!);
            if(!result.Success)
            {
                _logger.LogError("Failed to retrieve account '{UserId}' for password update: {Error}", UserId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to process the request. Please try again later." });
            }
            if (result.Data is null) return NotFound(new ErrorMessageResponse { Message = "User account not found." });
            var data = result.Data;
            if(BCrypt.Net.BCrypt.Verify(dto.OldPassword, data.Password)) return BadRequest(new ErrorMessageResponse { Message = "The current password is incorrect." });
            if(dto.Password != dto.ConfirmPassword) return BadRequest(new ErrorMessageResponse { Message = "New passwords do not match." });

            var updateResult = await _repository.UpdatePassword(UserId!, dto.Password);
            if(updateResult.Success)
            {
                return NoContent();
            }

            _logger.LogError("Failed to update password for account '{UserId}': {Error}", UserId, updateResult.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to update the password. Please try again later." });
        }

        /// <summary>
        /// Get My Projects
        /// </summary>
        /// <remarks>
        /// Returns the list of projects the authenticated user is a member of.
        /// The user ID is taken from the JWT — no query parameter is required.
        /// Returns <c>204 No Content</c> when the user is not a member of any project.
        /// Each item is mapped to a <see cref="ProjectItemResponse"/> summary.
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="ProjectItemResponse"/> on success;
        /// <see cref="NoContentResult"/> when the user has no projects;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the list of projects the user belongs to.</response>
        /// <response code="204">The user is not a member of any project.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet("projects", Name = "GetMyProjects")]
        [Authorize]
        [ProducesResponseType(typeof(List<ProjectItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyProjects()
        {
            var result = await _projectRepository.GetAllByUserId(UserId!);
            if(!result.Success)
            {
                _logger.LogError("Failed to retrieve projects for user '{UserId}': {Error}", UserId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve your projects. Please try again later." });
            }
            var data = result.Data?.Select(p => new ProjectItemResponse(p)).ToList();
            return data is null ? NoContent() : Ok(data);
        }

        /// <summary>
        /// Get My Tasks
        /// </summary>
        /// <remarks>
        /// Returns all tasks assigned to the authenticated user, grouped by project.
        /// The user ID is taken from the JWT — no query parameter is required.
        /// Results are grouped by the parent project of each task's column.
        /// Tasks whose column or project cannot be resolved are grouped under
        /// <c>"Unknown Project"</c>.
        /// Returns <c>204 No Content</c> when the user has no assigned tasks.
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="AllMyTasksResponse"/> grouped by project on success;
        /// <see cref="NoContentResult"/> when no tasks are assigned to the user;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the user's assigned tasks grouped by project.</response>
        /// <response code="204">The user has no assigned tasks.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet("tasks", Name = "GetMyTasks")]
        [Authorize]
        [ProducesResponseType(typeof(List<AllMyTasksResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetMyTasks()
        {
            var result = await _taskUserRepository.GetFullByUserId(UserId!);
            if(!result.Success)
            {
                _logger.LogError("Failed to retrieve tasks for user '{UserId}': {Error}", UserId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to retrieve your tasks. Please try again later." });
            }
            if(result.Data is null) return NoContent();

            var data = result.Data
                .GroupBy(t => new
                {
                    ProjectId = t.Task.Column?.ProjectId,
                    ProjectName = t.Task.Column?.Project?.Name ?? "Unknown Project"
                })
                .Select(group => new AllMyTasksResponse
                {
                    ProjectId = group.Key.ProjectId!.ToString(),
                    ProjectName = group.Key.ProjectName,
                    Tasks = group.Select(t => new Models.DTO.Response.Task.MyTaskResponse
                    {
                        Id = t.Task.Id,
                        Name = t.Task.Name,
                        Description = t.Task.Description,
                        Status = t.Task.Status,
                        Priority = t.Task.Priority,
                        CreateAt = t.Task.CreatedAt,
                        ColumnId = t.Task.ColumnId
                    }).ToList()
                }).ToList();

            return Ok(data);
        }

        /// <summary>
        /// Delete User
        /// </summary>
        /// <remarks>
        /// Permanently deletes a user account. Restricted to administrators and owners.
        /// The following rules are enforced before deletion:
        /// <list type="bullet">
        ///   <item><description>A user cannot delete their own account — returns <c>400</c>.</description></item>
        ///   <item><description>The <c>OWNER</c> account cannot be deleted by anyone — returns <c>403</c>.</description></item>
        ///   <item><description>An <c>ADMIN</c> account can only be deleted by the <c>OWNER</c> — returns <c>403</c> for <c>ADMIN</c> callers.</description></item>
        /// </list>
        /// </remarks>
        /// <param name="userId">The unique identifier of the user to delete.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="BadRequestObjectResult"/> with <see cref="ErrorMessageResponse"/> when attempting self-deletion;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when role-based deletion rules are violated;
        /// <see cref="NotFoundObjectResult"/> with <see cref="ErrorMessageResponse"/> when the target user does not exist;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">User account deleted successfully.</response>
        /// <response code="400">Cannot delete your own account.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">Deletion is forbidden by role rules (target is OWNER, or target is ADMIN and caller is not OWNER).</response>
        /// <response code="404">No user found for the provided identifier.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpDelete("{userId}", Name = "DeleteUser")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (UserId == userId) return BadRequest(new ErrorMessageResponse { Message = "You cannot delete your own account." });
            var result = await _repository.GetById(userId);
            if(!result.Success)
            {
                _logger.LogError("Failed to retrieve user '{TargetUserId}' for deletion: {Error}", userId, result.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Unable to process the request. Please try again later." });
            }
            if (result.Data is null) return NotFound(new ErrorMessageResponse { Message = "User not found." });
            if (result.Data.Role == Models.Enums.WorkspaceRoles.OWNER) return Forbid();
            if (result.Data.Role == Models.Enums.WorkspaceRoles.ADMIN && !IsWorkspaceOwner()) return Forbid();
            var deleteResult = await _repository.Delete(userId);
            if(deleteResult.Success)
            {
                return NoContent();
            }

            _logger.LogError("Failed to delete user '{TargetUserId}': {Error}", userId, deleteResult.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to delete the user account. Please try again later." });
        }
    }
}
