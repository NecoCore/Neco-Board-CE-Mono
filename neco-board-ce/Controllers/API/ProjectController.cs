using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Request.Projects;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.DTO.Response.Projects;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Services.Logs;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    /// <summary>
    /// Provides endpoints for managing workspace projects: listing, retrieval, creation, update, and deletion.
    /// </summary>
    /// <remarks>
    /// All endpoints require authentication via the class-level <c>[Authorize]</c> attribute.
    /// Most write operations additionally require the <c>ADMIN</c> or <c>OWNER</c> role.
    /// Repository failures return <c>500 Internal Server Error</c> instead of <c>400</c>,
    /// because errors at this layer indicate infrastructure problems, not invalid client input.
    /// Successful mutations broadcast real-time SignalR events to the affected project group
    /// and/or the global admins group.
    /// </remarks>
    [ApiController]
    [Authorize]
    [Route("api/project")]
    [Tags("Projects")]
    public class ProjectController : UserAuth
    {
        private readonly ILogger<ProjectController> _logger;
        private readonly ProjectRepository _repository;
        private readonly UserProjectRoleRepository _userProjectReposirory;
        private readonly AuditService _auditService;
        private readonly IRealtimeNotifier _notifier;
        private readonly UserAccessCheck _userAccess;

        public ProjectController(
            ILogger<ProjectController> logger,
            ProjectRepository repository,
            UserProjectRoleRepository userProject,
            AuditService auditService,
            IRealtimeNotifier notifier,
            UserAccessCheck userAccess
            )
        {
            _logger = logger;
            _repository = repository;
            _auditService = auditService;
            _notifier = notifier;
            _userProjectReposirory = userProject;
            _userAccess = userAccess;
        }

        /// <summary>
        /// Get All Projects
        /// </summary>
        /// <remarks>
        /// Returns a summary list of all projects in the workspace. Restricted to administrators and owners.
        /// Returns <c>204 No Content</c> when the repository succeeds but returns no data
        /// (i.e. no projects exist yet).
        /// Returns <c>500</c> when the repository itself fails — this indicates an infrastructure
        /// problem rather than a client error.
        /// </remarks>
        /// <returns>
        /// <see cref="OkObjectResult"/> with a list of <see cref="ProjectItemResponse"/> on success;
        /// <see cref="NoContentResult"/> when no projects exist;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks the ADMIN or OWNER role;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the list of all projects.</response>
        /// <response code="204">No projects exist in the workspace.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not hold the ADMIN or OWNER role.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet(Name = "GetAllProjects")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(typeof(List<ProjectItemResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAllProjects()
        {
            var result = await _repository.GetAll();
            if (result.Success)
            {
                var data = result.Data?.Select(p => new ProjectItemResponse(p)).ToList();
                if (data == null || data.Count == 0) return NoContent();
                return Ok(data);
            }
            _logger.LogError("Failed to retrieve all projects: {Error}", result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the project list. Please try again later." });
        }

        /// <summary>
        /// Get Project By Id
        /// </summary>
        /// <remarks>
        /// Returns the full details of a single project by its identifier.
        /// Access requires project membership (any role) or workspace administrator privileges.
        /// Returns <c>404</c> with an <see cref="ErrorMessageResponse"/> body when the repository
        /// succeeds but finds no record for <paramref name="id"/>.
        /// Returns <c>500</c> when the repository itself fails.
        /// </remarks>
        /// <param name="id">The unique identifier of the project to retrieve.</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with the <see cref="ProjectDetailResponse"/> on success;
        /// <see cref="NotFoundObjectResult"/> with <see cref="ErrorMessageResponse"/> when not found;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller is not a project member and is not a workspace admin;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="200">Returns the detailed project information.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller is not a project member and is not a workspace administrator.</response>
        /// <response code="404">No project found for the provided identifier.</response>
        /// <response code="500">Repository or infrastructure failure. Response body contains the error description.</response>
        [HttpGet("{id:guid}", Name = "GetProjectById")]
        [ProducesResponseType(typeof(ProjectDetailResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectById(Guid id)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!.Value, id);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetById(id);
            if (result.Success)
            {
                if (result.Data is null)
                    return NotFound(new ErrorMessageResponse { Message = $"Project '{id}' was not found." });

                return Ok(new ProjectDetailResponse(result.Data));
            }
            _logger.LogError("Failed to retrieve project '{ProjectId}': {Error}", id, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Unable to retrieve the project. Please try again later." });
        }

        /// <summary>
        /// Create Project
        /// </summary>
        /// <remarks>
        /// Creates a new project and assigns the creator as its owner. Restricted to administrators and owners.
        /// The authenticated user is automatically set as both <c>OwnerId</c> on the project entity
        /// and the first member with the <c>OWNER</c> role via <c>UserProjectRoleRepository</c>.
        /// If the role-assignment step fails, a warning is logged but the endpoint still returns
        /// <c>200 OK</c> with the new project ID — the project itself was created successfully.
        /// On success, broadcasts <c>SOCKET_EVENT_PROJECT_CREATED</c> to the admins SignalR group
        /// and, when role assignment succeeded, also to the creating user's personal connection.
        /// Returns <c>500</c> only when the project creation itself fails.
        /// </remarks>
        /// <param name="dto">Request body containing the project name and optional description.</param>
        /// <returns>
        /// <see cref="OkObjectResult"/> with <see cref="CreateProjectRequest"/> containing the new project ID on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks the ADMIN or OWNER role;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on project creation failure.
        /// </returns>
        /// <response code="200">Project created successfully. Response body contains the new project ID.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not hold the ADMIN or OWNER role.</response>
        /// <response code="500">Failed to persist the project. Response body contains the error description.</response>
        [HttpPost(Name = "CreateProject")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(typeof(CreateProjectRequest), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateProject([FromBody] ProjectRequest dto)
        {
            var project = new Project
            {
                OwnerId = UserId!.Value,
                Name = dto.Name,
                Description = dto.Description,
            };

            var createdResult = await _repository.Create(project);
            var addedResult = await _userProjectReposirory.AddToProject(UserId!.Value, project.Id, Models.Enums.ProjectRole.OWNER);

            if(!createdResult.Success)
            {
                _logger.LogError("Failed to create project '{ProjectName}': {Error}", dto.Name, createdResult.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = $"Failed to create project '{dto.Name}'. Please try again later." });
            }
            if(!addedResult.Success)
            {
                _logger.LogWarning("Failed to assign OWNER role to user '{UserId}' in project '{ProjectId}': {Error}", UserId, project.Id, createdResult.Message ?? "unknown error");
            }

            await _auditService.ProjectLog(project.Id, UserId!.Value, LogType.CREATED, "Project created", $"Name: {project.Name}");
            await _notifier.ProjectCreated();
            return Ok(new CreateProjectRequest { ProjectId = project.Id });
        }

        /// <summary>
        /// Update Project
        /// </summary>
        /// <remarks>
        /// Updates the name, description, and owner of an existing project. Restricted to administrators and owners.
        /// On success, broadcasts the <c>SOCKET_EVENT_PROJECT_UPDATED</c> SignalR event
        /// to both the project's own group and the global admins group.
        /// The admins group event carries the project ID as payload.
        /// When <c>dto.OwnerId</c> is <c>null</c>, the owner field is set to an empty string.
        /// Returns <c>500</c> when the repository fails to persist the update.
        /// </remarks>
        /// <param name="id">The unique identifier of the project to update.</param>
        /// <param name="dto">Request body containing the new name, description, and optional owner ID.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks the ADMIN or OWNER role;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">Project updated successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not hold the ADMIN or OWNER role.</response>
        /// <response code="500">Failed to persist the update. Response body contains the error description.</response>
        [HttpPut("{id:guid}", Name = "UpdateProject")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(Guid id, [FromBody] ProjectUpdateRequest dto)
        {
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                IsArchived = dto.IsArchived,
                OwnerId = dto.OwnerId ?? Guid.Empty
            };

            var result = await _repository.Update(id, project);
            if (result.Success)
            {
                await _auditService.ProjectLog(id, UserId!.Value, LogType.EDITED, "Project updated", $"New Name: {dto.Name}");
                await _notifier.ProjectUpdated(id, dto.Name);
                return NoContent();
            }

            _logger.LogError("Failed to update project '{ProjectId}': {Error}", id, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = $"Failed to update project '{id}'. Please try again later." });
        }

        /// <summary>
        /// Delete Project
        /// </summary>
        /// <remarks>
        /// Permanently deletes a project by its identifier. Restricted to administrators and owners.
        /// On success, broadcasts two SignalR events:
        /// <list type="bullet">
        ///   <item><description><c>SOCKET_EVENT_PROJECT_DELETED</c> — sent to the project's own group (notifies all project members).</description></item>
        ///   <item><description><c>SOCKET_EVENT_PROJECT_DELETED</c> — sent to the global all-users group with the project ID as payload.</description></item>
        /// </list>
        /// Returns <c>500</c> when the repository fails to delete the record.
        /// </remarks>
        /// <param name="id">The unique identifier of the project to delete.</param>
        /// <returns>
        /// <see cref="NoContentResult"/> on success;
        /// <see cref="UnauthorizedResult"/> when the caller is not authenticated;
        /// <see cref="ForbidResult"/> when the caller lacks the ADMIN or OWNER role;
        /// <see cref="StatusCodeResult"/> 500 with <see cref="ErrorMessageResponse"/> on repository failure.
        /// </returns>
        /// <response code="204">Project deleted successfully.</response>
        /// <response code="401">The request is not authenticated.</response>
        /// <response code="403">The caller does not hold the ADMIN or OWNER role.</response>
        /// <response code="500">Failed to delete the project. Response body contains the error description.</response>
        [HttpDelete("{id:guid}", Name = "DeleteProject")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(Guid id)
        {
            var result = await _repository.Delete(id);
            if (result.Success)
            {
                await _auditService.ProjectLog(id, UserId!.Value, LogType.DELETED, "Project deleted");
                await _notifier.ProjectDeleted(id);
                return NoContent();
            }

            _logger.LogError("Failed to delete project '{ProjectId}': {Error}", id, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = $"Failed to delete project '{id}'. Please try again later." });
        }
    }
}
