using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
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
    /// <summary>
    /// Provides endpoints for managing columns within a project: listing, creation, renaming, reordering, and deletion.
    /// </summary>
    /// <remarks>
    /// All endpoints require authentication via the class-level <c>[Authorize]</c> attribute.
    /// Write operations require at least the <c>MODERATOR</c> role in the target project,
    /// or workspace administrator privileges.
    /// Repository failures return <c>500 Internal Server Error</c> to distinguish infrastructure
    /// problems from invalid client input.
    /// Successful mutations broadcast real-time SignalR events to the parent project group.
    /// </remarks>
    [ApiController]
    [Authorize]
    [Route("api/column")]
    [Tags("Project columns")]
    public class ColumsProjectController : UserAuth
    {
        private readonly ILogger<ColumsProjectController> _logger;
        private readonly ColumnsRepository _repository;
        private readonly IRealtimeNotifier _notifier;
        private readonly UserAccessCheck _userAccess;

        public ColumsProjectController(
            ILogger<ColumsProjectController> logger,
            ColumnsRepository repository,
            IRealtimeNotifier notifier,
            UserAccessCheck userAccess
            )
        {
            _logger = logger;
            _repository = repository;
            _userAccess = userAccess;
            _notifier = notifier;
        }

        /// <summary>
        /// Get Columns In Project
        /// </summary>
        /// <remarks>
        /// Returns the ordered list of columns belonging to the specified project.
        /// Access requires any project membership or workspace administrator privileges.
        /// Returns <c>204 No Content</c> when the repository succeeds but the project
        /// has no columns yet (data payload is <c>null</c>).
        /// Each item is mapped to a <see cref="ColumnItemResponse"/> summary.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project whose columns are requested.</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="ColumnItemResponse"/> on success;
        /// <see cref="NoContentResult"/> when the project has no columns;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller is not a project member and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the list of columns in the project.</response>
        /// <response code="204">The project exists but has no columns.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not a project member and is not a workspace administrator.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
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
            _logger.LogError("Failed to retrieve columns for project '{ProjectId}': {Error}", projectId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the column list. Please try again later." });
        }

        /// <summary>
        /// Create Column In Project
        /// </summary>
        /// <remarks>
        /// Creates a new column at the end of the column sequence for the specified project.
        /// Requires at least the <c>MODERATOR</c> role in the project, or workspace administrator privileges.
        /// The new column's <c>Queue</c> position is calculated as <c>max(existing queues) + 1</c>,
        /// or <c>1</c> if the project currently has no columns.
        /// The existing column list is fetched first; if that fetch fails (<c>400</c>) or returns
        /// no data (<c>404</c>), the creation is aborted.
        /// On success, broadcasts <c>SOKET_EVENT_COLUMN_CREATED</c> to the project's SignalR group.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project to add the column to.</param>
        /// <param name="dto">Request body containing the name of the new column.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="BadRequestObjectResult"/> with <see cref="ErrorMessageResponse"/> when the column list fetch fails;
        /// <see cref="NotFoundObjectResult"/> with <see cref="ErrorMessageResponse"/> when the project returns no column data;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks MODERATOR role and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on column creation failure.
        /// </returns>
        /// <response code="204">Column created successfully.</response>
        /// <response code="400">Failed to retrieve existing columns before creation. Response body contains the error description.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have MODERATOR role and is not a workspace administrator.</response>
        /// <response code="404">No column data found for the specified project.</response>
        /// <response code="500">Failed to persist the new column. Response body contains the error description.</response>
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
                await _notifier.ColumnCreated(projectId);
                return NoContent();
            }
            _logger.LogError("Failed to create column in project '{ProjectId}': {Error}", projectId, createResult.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to create the column. Please try again later." });
        }

        /// <summary>
        /// Update Column
        /// </summary>
        /// <remarks>
        /// Updates the name of an existing column.
        /// Requires at least the <c>MODERATOR</c> role in the column's parent project,
        /// or workspace administrator privileges.
        /// On success, broadcasts <c>SOKET_EVENT_COLUMN_UPDATED</c> to the project's SignalR group.
        /// </remarks>
        /// <param name="columnId">The unique identifier of the column to update.</param>
        /// <param name="dto">Request body containing the new column name.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks MODERATOR role and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">Column name updated successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have MODERATOR role and is not a workspace administrator.</response>
        /// <response code="500">Failed to persist the update. Response body contains the error description.</response>
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
                await _notifier.ColumnUpdated(accessResult.ProjectId!, updateResult.Message!, dto.Name);
                return NoContent();
            }

            _logger.LogError("Failed to update column '{ColumnId}': {Error}", columnId, updateResult.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to update the column. Please try again later." });
        }

        /// <summary>
        /// Update Column Order
        /// </summary>
        /// <remarks>
        /// Updates the display order (queue position) of a column within its project.
        /// Requires at least the <c>MODERATOR</c> role in the column's parent project,
        /// or workspace administrator privileges.
        /// The project ID is resolved internally from the access check result and passed
        /// directly to the repository — it is not required in the request.
        /// On success, broadcasts <c>SOKET_EVENT_COLUMN_UPDATED_ORDER</c> to the project's SignalR group.
        /// </remarks>
        /// <param name="columnId">The unique identifier of the column to reorder.</param>
        /// <param name="queue">The new queue position (display order index) for the column.</param>
        /// <returns>
        /// <see cref="OkResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks MODERATOR role and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Column order updated successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have MODERATOR role and is not a workspace administrator.</response>
        /// <response code="500">Failed to persist the new order. Response body contains the error description.</response>
        [HttpPut("{columnId}/order", Name = "UpdateColumnOrder")]
        [ProducesResponseType(StatusCodes.Status200OK)]
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
                await _notifier.ColumnOrderUpdated(accessResult.ProjectId!);
                return Ok();
            }

            _logger.LogError("Failed to update order for column '{ColumnId}': {Error}", columnId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to update the column order. Please try again later." });
        }

        /// <summary>
        /// Delete Column
        /// </summary>
        /// <remarks>
        /// Permanently deletes a column and all its contents from the project.
        /// Requires at least the <c>MODERATOR</c> role in the parent project,
        /// or workspace administrator privileges.
        /// <paramref name="projectId"/> is a query string parameter used to resolve
        /// project membership and to target the SignalR broadcast group.
        /// On success, broadcasts <c>SOKET_EVENT_COLUMN_DELETED</c> to the project's SignalR group.
        /// </remarks>
        /// <param name="columnId">The unique identifier of the column to delete (route parameter).</param>
        /// <param name="projectId">The unique identifier of the parent project (query parameter).</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks MODERATOR role and is not a workspace administrator;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">Column deleted successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not have MODERATOR role and is not a workspace administrator.</response>
        /// <response code="500">Failed to delete the column. Response body contains the error description.</response>
        [HttpDelete("{columnId}", Name = "DeleteColumn")]
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
                await _notifier.ColumnDelete(projectId, columnId);
                return NoContent();
            }

            _logger.LogError("Failed to delete column '{ColumnId}': {Error}", columnId, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Failed to delete the column. Please try again later." });
        }
    }
}
