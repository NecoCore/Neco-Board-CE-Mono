using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Models.DTO.Response.Logs;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Provides endpoints for retrieving audit logs.
    /// Access is restricted to workspace administrators or project members (for project-specific logs).
    /// </summary>
    [ApiController]
    [Authorize]
    [Route("api/logs")]
    [Tags("Audit logs")]
    public class LogsController : UserAuth
    {
        private readonly LogsRepository _repository;
        private readonly UserAccessCheck _userAccess;
        private readonly ILogger<LogsController> _logger;

        public LogsController(LogsRepository repository, UserAccessCheck userAccess, ILogger<LogsController> logger)
        {
            _repository = repository;
            _userAccess = userAccess;
            _logger = logger;
        }

        /// <summary>
        /// Get Global Audit Logs
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of all audit logs in the workspace.
        /// Restricted to workspace administrators (ADMIN or OWNER).
        /// </remarks>
        /// <param name="count">Items per page (default: 50).</param>
        /// <param name="page">Page number (default: 1).</param>
        /// <response code="200">Returns the list of logs.</response>
        /// <response code="204">No logs found for the requested page.</response>
        /// <response code="403">The caller is not a workspace administrator.</response>
        /// <response code="500">Database failure.</response>
        [HttpGet("all", Name = "GetGlobalLogs")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(typeof(List<LogItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetGlobalLogs([FromQuery] int count = 50, [FromQuery] int page = 1)
        {
            var result = await _repository.GetPage(count, page);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ErrorMessageResponse { Message = result.Message ?? "Failed to retrieve logs." });
            }

            if (result.Data == null || result.Data.Count == 0) return NoContent();

            var data = result.Data.Select(l => new LogItemResponse(l)).ToList();
            return Ok(data);
        }

        /// <summary>
        /// Get Project Audit Logs
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of audit logs for a specific project.
        /// Access requires project membership or workspace administrator privileges.
        /// </remarks>
        /// <param name="projectId">The unique identifier of the project.</param>
        /// <param name="count">Items per page (default: 50).</param>
        /// <param name="page">Page number (default: 1).</param>
        /// <response code="200">Returns the list of logs.</response>
        /// <response code="204">No logs found for the project or the requested page.</response>
        /// <response code="403">The caller has no access to the project.</response>
        /// <response code="500">Database failure.</response>
        [HttpGet("project/{projectId:guid}", Name = "GetProjectLogs")]
        [ProducesResponseType(typeof(List<LogItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectLogs(Guid projectId, [FromQuery] int count = 50, [FromQuery] int page = 1)
        {
            var access = await _userAccess.HasAccessToProject(UserId!.Value, projectId);
            if (!access.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetByProjectId(projectId, count, page);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ErrorMessageResponse { Message = result.Message ?? "Failed to retrieve logs." });
            }

            if (result.Data == null || result.Data.Count == 0) return NoContent();

            var data = result.Data.Select(l => new LogItemResponse(l)).ToList();
            return Ok(data);
        }

        /// <summary>
        /// Get User Activity Logs
        /// </summary>
        /// <remarks>
        /// Returns a paginated list of audit logs for actions performed by a specific user.
        /// Restricted to workspace administrators.
        /// </remarks>
        /// <param name="userId">The unique identifier of the user.</param>
        /// <param name="count">Items per page (default: 50).</param>
        /// <param name="page">Page number (default: 1).</param>
        /// <response code="200">Returns the list of logs.</response>
        /// <response code="204">No logs found for the user.</response>
        /// <response code="403">The caller is not a workspace administrator.</response>
        /// <response code="500">Database failure.</response>
        [HttpGet("user/{userId:guid}", Name = "GetUserLogs")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(typeof(List<LogItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetUserLogs(Guid userId, [FromQuery] int count = 50, [FromQuery] int page = 1)
        {
            var result = await _repository.GetByUserId(userId, count, page);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, 
                    new ErrorMessageResponse { Message = result.Message ?? "Failed to retrieve logs." });
            }

            if (result.Data == null || result.Data.Count == 0) return NoContent();

            var data = result.Data.Select(l => new LogItemResponse(l)).ToList();
            return Ok(data);
        }
    }
}
