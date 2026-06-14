using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Attributes.ProjectAccessAttribute;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Request.Tasks;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.DTO.Response.Task;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Services.Logs;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Provides endpoints for reading and modifying task details,
    /// including status, priority, and user assignments.
    /// </summary>
    /// <remarks>
    /// All endpoints require authentication via the <c>[Authorize]</c> attribute.
    /// Access to each operation is controlled by the caller's project role
    /// (<see cref="ProjectRole"/>) or workspace administrator privileges.
    /// Successful mutations broadcast real-time SignalR events to both the
    /// affected task group and the parent project group.
    /// </remarks>
    [ApiController()]
    [Authorize]
    [Route("api/tasks/{taskId:guid}")]
    [Tags("Task information")]
    public class TaskInfoController : UserAuth
    {
        private readonly ILogger<TaskInfoController> _logger;
        private readonly ColumnTaskRepository _repository;
        private readonly TaskUserRepository _taskUserRepository;
        private readonly UserAccessCheck _userAccess;
        private readonly AuditService _auditService;
        private readonly IRealtimeNotifier _notifier;

        public TaskInfoController(
            ILogger<TaskInfoController> logger, 
            UserAccessCheck userAccess, 
            ColumnTaskRepository repository, 
            TaskUserRepository taskUserRepository, 
            AuditService auditService,
            IRealtimeNotifier notifier)
        {
            _logger = logger;
            _repository = repository;
            _userAccess = userAccess;
            _taskUserRepository = taskUserRepository;
            _auditService = auditService;
            _notifier = notifier;
        }

        /// <summary>
        /// Update Task Status
        /// </summary>
        /// <remarks>
        /// Updates the status of a task.
        /// On success, broadcasts the <c>SOCKET_EVENT_TASK_STATUS_UPDATED</c> SignalR event
        /// to both the task group (identified by <paramref name="taskId"/>) and the parent
        /// project group. The project group receives the new status value as the event payload.
        /// <br/><br/>
        /// Requires at least <see cref="ProjectRole.VIEWER"/> membership in the project,
        /// or workspace administrator privileges.
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task to update.</param>
        /// <param name="dto">Request body containing the new status value.</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Task status updated successfully.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have VIEWER role in the project and is not a workspace administrator.</response>
        [HttpPatch("status", Name = "UpdateTaskStatus")]
        [ProjectAccess(ProjectRole.VIEWER)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateStatus(Guid taskId, [FromBody] EditTaskStatusRequest dto)
        {
            var result = await _repository.UpdateStatus(taskId, dto.Status);

            if (result.Success)
            {
                await _auditService.TaskLog(CurrentProjectId!.Value, UserId!.Value, LogType.EDITED, "Task status updated", $"TaskId: {taskId}, New Status: {dto.Status}");
                await _notifier.TaskStatusUpdated(CurrentProjectId!.Value, taskId, result.Data, dto.Status);
                return Ok();
            }
            _logger.LogError("Failed to update status in {taskId}: {error}", taskId, result.Message ?? "unknown error");
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }

        /// <summary>
        /// Update Task Priority
        /// </summary>
        /// <remarks>
        /// Updates the priority of a task.
        /// On success, emits the <c>TaskPriorityUpdated</c> SignalR event to both the task group
        /// (identified by <paramref name="taskId"/>) and the parent project group, with a
        /// <c>TaskPriorityUpdatedResponse</c> payload (task id, column id and the new priority).
        /// <br/><br/>
        /// Requires at least <see cref="ProjectRole.VIEWER"/> membership in the project,
        /// or workspace administrator privileges.
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task to update.</param>
        /// <param name="dto">Request body containing the new priority value.</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Task priority updated successfully.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have VIEWER role in the project and is not a workspace administrator.</response>
        [HttpPatch("priority", Name = "UpdateTaskPriority")]
        [ProjectAccess(ProjectRole.VIEWER)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdatePriority(Guid taskId, [FromBody] EditTaskPriorityRequest dto)
        {
            var result = await _repository.UpdatePriority(taskId, dto.Priority);

            if (result.Success)
            {
                await _auditService.TaskLog(CurrentProjectId!.Value, UserId!.Value, LogType.EDITED, "Task priority updated", $"TaskId: {taskId}, New Priority: {dto.Priority}");
                await _notifier.TaskPriorityUpdated(CurrentProjectId!.Value, taskId, result.Data, dto.Priority);
                return Ok();
            }
            _logger.LogError("Failed to update priority in {taskId}: {error}", taskId, result.Message ?? "unknown error");
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }

        /// <summary>
        /// Get All Users In Task
        /// </summary>
        /// <remarks>
        /// Returns all users assigned to the specified task.
        /// Any project member (any role) or a workspace administrator may call this endpoint.
        /// When no users are assigned, the repository returns an empty list and the response
        /// is still <c>200 OK</c> — the endpoint never returns <c>204 No Content</c>.
        /// </remarks>
        /// <param name="projectId">Project identifier (reserved; currently unused in the query).</param>
        /// <param name="taskId">The unique identifier of the task whose assigned users are requested.</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> containing a <see cref="List{TaskUser}"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Returns the list of users assigned to the task. The list may be empty.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not a project member and is not a workspace administrator.</response>
        [HttpGet("user", Name = "GetAllUsersInTask")]
        [ProjectAccess]
        [ProducesResponseType(typeof(List<TaskUser>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetUsers(Guid projectId, Guid taskId)
        {
            var users = await _taskUserRepository.GetByTaskId(taskId);
            if(!users.Success)
            {
                _logger.LogError("Failed to fetch users in {taskId}: {error}", taskId, users.Message ?? "unknown error");
                return BadRequest(new ErrorMessageResponse { Message = users.Message ?? "unknown error" });
            }
            if (users.Data == null || users.Data.Count == 0) return NoContent();
            return Ok(users.Data);
        }

        /// <summary>
        /// Add User In Task
        /// </summary>
        /// <remarks>
        /// Assigns a user to the specified task.
        /// A two-tier access check is enforced:
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       The caller must have at least <see cref="ProjectRole.VIEWER"/> membership,
        ///       or be a workspace administrator — otherwise <c>403</c> is returned immediately.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       To assign a <em>different</em> user (i.e. <c>dto.UserId</c> is not <c>null</c>),
        ///       the caller must additionally have at least <see cref="ProjectRole.USER"/> membership
        ///       — otherwise <c>403</c> is returned.
        ///     </description>
        ///   </item>
        /// </list>
        /// When <c>dto.UserId</c> is <c>null</c>, the currently authenticated user assigns themselves.
        /// On success, broadcasts the <c>SOCKET_EVENT_TASK_USER_ADDED</c> event to the task's SignalR group.
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="dto">
        /// Request body. When <c>UserId</c> is <c>null</c> the caller is assigned to the task;
        /// otherwise the specified user is assigned (requires at least USER role).
        /// </param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when either access check fails;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">User successfully assigned to the task.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">
        /// Returned in two cases: (1) the caller lacks VIEWER membership and is not a workspace admin;
        /// (2) the caller lacks USER role and is attempting to assign another user.
        /// </response>
        [HttpPost("user", Name = "AddUserInTask")]
        [ProjectAccess(ProjectRole.VIEWER)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> AddUser(Guid taskId, [FromBody] AddUserInTaskRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToTask(UserId!.Value, taskId, ProjectRole.USER);
            if (!accessResult.Result && dto.UserId is not null) return Forbid();

            Guid targetUserId = dto.UserId ?? UserId!.Value;
            var result = await _taskUserRepository.AddUser(taskId, targetUserId);

            if (result.Success)
            {
                await _auditService.TaskLog(CurrentProjectId!.Value, UserId!.Value, LogType.EDITED, "User assigned to task", $"TaskId: {taskId}, Target UserId: {targetUserId}");
                await _notifier.TaskAddUser(taskId);
                return Ok();
            }
            _logger.LogError("Failed to update user in {taskId}: {error}", taskId, result.Message ?? "unknown error");
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }

        /// <summary>
        /// Remove User From Task
        /// </summary>
        /// <remarks>
        /// Removes a user from the specified task.
        /// A two-tier access check is enforced — identical to <see cref="AddUser"/>:
        /// <list type="number">
        ///   <item>
        ///     <description>
        ///       The caller must have at least <see cref="ProjectRole.VIEWER"/> membership,
        ///       or be a workspace administrator — otherwise <c>403</c> is returned immediately.
        ///     </description>
        ///   </item>
        ///   <item>
        ///     <description>
        ///       To remove a <em>different</em> user (i.e. <c>dto.UserId</c> is not <c>null</c>),
        ///       the caller must additionally have at least <see cref="ProjectRole.USER"/> membership
        ///       — otherwise <c>403</c> is returned.
        ///     </description>
        ///   </item>
        /// </list>
        /// When <c>dto.UserId</c> is <c>null</c>, the currently authenticated user removes themselves.
        /// On success, broadcasts the <c>SOCKET_EVENT_TASK_USER_REMOVED</c> event to the task's SignalR group.
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task.</param>
        /// <param name="dto">
        /// Request body. When <c>UserId</c> is <c>null</c> the caller is removed from the task;
        /// otherwise the specified user is removed (requires at least USER role).
        /// </param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when either access check fails;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">User successfully removed from the task.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">
        /// Returned in two cases: (1) the caller lacks VIEWER membership and is not a workspace admin;
        /// (2) the caller lacks USER role and is attempting to remove another user.
        /// </response>
        [HttpDelete("user", Name = "RemoveUserFromTask")]
        [ProjectAccess(ProjectRole.VIEWER)]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> RemoveUser(Guid taskId, [FromBody] AddUserInTaskRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToTask(UserId!.Value, taskId, ProjectRole.USER);
            if (!accessResult.Result && dto.UserId is not null) return Forbid();

            Guid targetUserId = dto.UserId ?? UserId!.Value;
            var result = await _taskUserRepository.RemoveUser(taskId, targetUserId);

            if (result.Success)
            {
                await _auditService.TaskLog(CurrentProjectId!.Value, UserId!.Value, LogType.EDITED, "User unassigned from task", $"TaskId: {taskId}, Target UserId: {targetUserId}");
                await _notifier.TaskRemoveUser(taskId, targetUserId);
                return Ok();
            }
            _logger.LogError("Failed to delete user from {taskId}: {error}", taskId, result.Message ?? "unknown error");
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }
    }
}
