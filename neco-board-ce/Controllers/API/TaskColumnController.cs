using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Request.Tasks;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.DTO.Response.Task;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Provides endpoints for creating, reading, updating, and deleting tasks
    /// within project columns.
    /// </summary>
    /// <remarks>
    /// All endpoints require authentication via the <c>[Authorize]</c> attribute.
    /// Access is controlled by the caller's project role (<see cref="ProjectRole"/>)
    /// or workspace administrator privileges.
    /// Successful mutations broadcast real-time SignalR events to the parent project group.
    /// </remarks>
    [ApiController]
    [Authorize]
    [Route("api/tasks")]
    [Tags("Task column")]
    public class TaskColumnController : UserAuth
    {
        private readonly ILogger<TaskColumnController> _logger;
        private readonly ColumnTaskRepository _repository;
        private readonly TaskUserRepository _taskUserRepository;
        private readonly IRealtimeNotifier _notifier;
        private readonly UserAccessCheck _userAccess;

        public TaskColumnController(
            ILogger<TaskColumnController> logger, 
            UserAccessCheck userAccess, 
            ColumnTaskRepository repository, 
            TaskUserRepository taskUserRepository, 
            IRealtimeNotifier notifier)
        {
            _logger = logger;
            _repository = repository;
            _userAccess = userAccess;
            _taskUserRepository = taskUserRepository;
            _notifier = notifier;
        }

        /// <summary>
        /// Get Tasks In Column
        /// </summary>
        /// <remarks>
        /// Returns the list of tasks belonging to the specified column.
        /// Access requires any project membership for the column's parent project,
        /// or workspace administrator privileges.
        /// Returns <c>204 No Content</c> when the column exists but contains no tasks
        /// (repository returns a <c>null</c> data payload).
        /// Each item in the returned list contains only summary fields
        /// (<see cref="TaskResponse"/>), not the full task body text.
        /// </remarks>
        /// <param name="columnId">The unique identifier of the column whose tasks are requested.</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="TaskResponse"/> on success;
        /// <see cref="NoContentResult"/> when the column has no tasks;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Returns the list of tasks in the column.</response>
        /// <response code="204">The column exists but contains no tasks.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not a project member and is not a workspace administrator.</response>
        [HttpGet("in-column/{columnId}", Name = "GetTasksInColumn")]
        [ProducesResponseType(typeof(List<TaskResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetInColumn(string columnId)
        {
            var accessResult = await _userAccess.HasAccessToColumn(UserId!, columnId);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var tasks = await _repository.GetByColumnId(columnId);
            if (!tasks.Success) return BadRequest(new ErrorMessageResponse { Message = tasks.Message ?? "unknown error" });
            if (tasks.Data is null) return NoContent();

            var taskLitle = tasks.Data.Select(t => new TaskResponse
            {
                Id = t.Id,
                Name = t.Name,
                Description = t.Description,
                Priority = t.Priority,
                Status = t.Status,
                CreatedAt = t.CreatedAt,
                ColumnId = t.ColumnId
            }).ToList();

            return Ok(taskLitle);
        }

        /// <summary>
        /// Get Task By Id
        /// </summary>
        /// <remarks>
        /// Returns the full details of a single task by its identifier.
        /// Access requires any project membership for the task's parent project,
        /// or workspace administrator privileges.
        /// Returns <c>204 No Content</c> when the repository finds no record for
        /// the given <paramref name="taskId"/> (data payload is <c>null</c>).
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task to retrieve.</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with the <see cref="ColumnTask"/> entity on success;
        /// <see cref="NoContentResult"/> when no task exists for the given ID;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Returns the full task entity.</response>
        /// <response code="204">No task found for the provided identifier.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not a project member and is not a workspace administrator.</response>
        [HttpGet("{taskId}", Name = "GetTaskById")]
        [ProducesResponseType(typeof(ColumnTask), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTaskInfo(string taskId)
        {
            var accessResult = await _userAccess.HasAccessToTask(UserId!, taskId);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var task = await _repository.GetById(taskId);
            if (!task.Success) return BadRequest(new ErrorMessageResponse { Message = task.Message ?? "unknown error" });
            if (task.Data is null) return NoContent();

            return Ok(task.Data);
        }

        /// <summary>
        /// Create Task
        /// </summary>
        /// <remarks>
        /// Creates a new task in the specified column.
        /// The caller is automatically set as the task owner (<c>OwnerId</c>).
        /// Requires at least <see cref="ProjectRole.VIEWER"/> membership in the column's
        /// parent project, or workspace administrator privileges.
        /// On success, broadcasts the <c>SOCKET_EVENT_TASK_CREATED</c> SignalR event
        /// to the parent project group, passing the target column ID as the payload.
        /// </remarks>
        /// <param name="dto">Request body containing the column ID, name, description, and text of the new task.</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Task created successfully.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have VIEWER role in the project and is not a workspace administrator.</response>
        [HttpPost(Name = "CreateTask")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Create([FromBody] TaskColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToColumn(UserId!, dto.ColumnId, ProjectRole.VIEWER);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var task = new ColumnTask
            {
                ColumnId = dto.ColumnId,
                OwnerId = UserId!,
                Name = dto.Name,
                Description = dto.Description,
                Text = dto.Text,
            };
            var result = await _repository.Create(task);

            if(result.Success)
            {
                await _notifier.TaskCreated(accessResult.ProjectId!, dto.ColumnId);
                return Ok();
            }
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }

        /// <summary>
        /// Update Task
        /// </summary>
        /// <remarks>
        /// Replaces the content fields of an existing task (name, description, text).
        /// Only the <c>Name</c>, <c>Description</c>, and <c>Text</c> fields are updated;
        /// column assignment, status, and priority are not affected by this endpoint.
        /// Requires at least <see cref="ProjectRole.VIEWER"/> membership in the task's
        /// parent project, or workspace administrator privileges.
        /// On success, broadcasts the <c>SOCKET_EVENT_TASK_UPDATED</c> SignalR event
        /// to the parent project group with the task ID as the payload.
        /// </remarks>
        /// <param name="taskId">The unique identifier of the task to update.</param>
        /// <param name="dto">Request body containing the new name, description, and text values.</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Task content updated successfully.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have VIEWER role in the project and is not a workspace administrator.</response>
        [HttpPut("{taskId}", Name = "UpdateTask")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Update(string taskId, [FromBody] TaskColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToTask(UserId!, taskId, ProjectRole.VIEWER);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var task = new ColumnTask
            {
                OwnerId = UserId!,
                Name = dto.Name,
                Description = dto.Description,
                Text = dto.Text,
            };
            var result = await _repository.Update(taskId, task);

            if (result.Success)
            {
                await _notifier.TaskUpdated(accessResult.ProjectId!, taskId);
                return Ok();
            }
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }

        /// <summary>
        /// Move Task To Column
        /// </summary>
        /// <remarks>
        /// Moves a task to a different column within the same project.
        /// Requires at least <see cref="ProjectRole.VIEWER"/> membership in the task's
        /// parent project, or workspace administrator privileges.
        /// On success, emits the <c>TaskColumnUpdated</c> SignalR event to the project group
        /// identified by <paramref name="projectId"/>, carrying both the previous and the new
        /// column identifiers.
        /// <br/><br/>
        /// <paramref name="projectId"/> and <paramref name="columnId"/> are passed
        /// as query string parameters (not part of the route template).
        /// </remarks>
        /// <param name="projectId">The unique identifier of the parent project (query parameter), used to target the SignalR group.</param>
        /// <param name="taskId">The unique identifier of the task to move (route parameter).</param>
        /// <param name="columnId">The current column identifier of the task (query parameter), sent as <c>OldColumnId</c> in the SignalR payload.</param>
        /// <param name="dto">Request body containing the target column ID (<c>ColumnId</c>).</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Task moved to the new column successfully.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have VIEWER role in the project and is not a workspace administrator.</response>
        [HttpPatch("{taskId}/column", Name = "MoveTaskToColumn")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateColumn(string projectId, string taskId, string columnId, [FromBody] EditTaskColumnRequest dto)
        {
            var accessResult = await _userAccess.HasAccessToTask(UserId!, taskId, ProjectRole.VIEWER);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.MoveToColumn(taskId, dto.ColumnId);

            if (result.Success)
            {
                await _notifier.TaskColumnUpdated(projectId, columnId, dto.ColumnId);
                return Ok();
            }
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }

        /// <summary>
        /// Delete Task
        /// </summary>
        /// <remarks>
        /// Permanently deletes a task by its identifier.
        /// Requires at least <see cref="ProjectRole.VIEWER"/> membership in the task's
        /// parent project, or workspace administrator privileges.
        /// On success, two SignalR events are broadcast:
        /// <list type="bullet">
        ///   <item><description><c>SOCKET_EVENT_TASK_DELETED</c> — sent to the task's own group.</description></item>
        ///   <item><description><c>SOCKET_EVENT_TASK_PRIORITY_UPDATED</c> — sent to the project group with the task ID as payload.</description></item>
        /// </list>
        /// <paramref name="projectId"/> is passed as a query string parameter
        /// (not part of the route template) and is used to target the project SignalR group.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the parent project (query parameter), used to target the SignalR group.</param>
        /// <param name="taskId">The unique identifier of the task to delete (route parameter).</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="ForbidResult"/> when access is denied;
        /// <see cref="BadRequestObjectResult"/> when the repository operation fails.
        /// </returns>
        /// <response code="200">Task deleted successfully.</response>
        /// <response code="400">Repository or database operation failed. Response body contains the error message.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have VIEWER role in the project and is not a workspace administrator.</response>
        [HttpDelete("{taskId}", Name = "DeleteTask")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> DeleteTask(string projectId, string taskId)
        {
            var accessResult = await _userAccess.HasAccessToTask(UserId!, taskId, ProjectRole.VIEWER);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.Delete(taskId);

            if (result.Success)
            {
                await _notifier.TaskDelete(projectId, result.Message!, taskId);
                return Ok();
            }
            return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Unknown error" });
        }
    }
}
    

