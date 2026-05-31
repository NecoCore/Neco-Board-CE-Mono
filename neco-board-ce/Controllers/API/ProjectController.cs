using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.DTO.Response.Massages;
using neco_board_ce.Models.DTO.Response.Projects;
using neco_board_ce.Models.Entity;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Controllers;
using System.Security.Claims;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Authorize]
    [Route("api/project")]
    public class ProjectController : UserAuth
    {
        private readonly ILogger<ProjectController> _logger;
        private readonly ProjectRepository _repository;
        private readonly UserProjectRoleRepository _userProjectReposirory;
        private readonly IHubContext<AppHub> _appHubContext;
        private readonly IHubContext<ProjectHub> _projectHubContext;
        private readonly UserAccessCheck _userAccess;

        public ProjectController(
            ILogger<ProjectController> logger, 
            ProjectRepository repository, 
            UserProjectRoleRepository userProject, 
            IHubContext<AppHub> appHubContext, 
            IHubContext<ProjectHub> projectHubContext,
            UserAccessCheck userAccess
            )
        {
            _logger = logger;
            _repository = repository;
            _userProjectReposirory = userProject;
            _appHubContext = appHubContext;
            _projectHubContext = projectHubContext;
            _userAccess = userAccess;
        }

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
                return data is null ? NoContent() : Ok(data);
            }
            _logger.LogError("Failed to get all projects in admin route: {error}", result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "An internal server error occurred while receiving all projects." });
        }

        [HttpGet("{id}", Name = "GetProjectById")]
        [ProducesResponseType(typeof(Project), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetProjectById(string id)
        {
            var accessResult = await _userAccess.HasAccessToProject(UserId!, id);
            if (!accessResult.Result && !IsWorkspaceAdmin()) return Forbid();

            var result = await _repository.GetById(id);
            if (result.Success)
            {
                return result.Data is null ? 
                    NotFound(new ErrorMessageResponse { Message = $"Project with ID {id} not found." }) : 
                    Ok(result.Data);
            }
            _logger.LogError("Failed to get project {projectId}: {error}", id, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "An internal server error occurred while receiving the project." });
        }

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
                OwnerId = UserId,
                Name = dto.Name,
                Description = dto.Description,
            };

            var createdResult = await _repository.Create(project);
            var addedResult = await _userProjectReposirory.AddToProject(UserId, project.Id, Models.Enums.ProjectRole.OWNER);

            if(!createdResult.Success)
            {
                _logger.LogError("Failed to create a project '{projectName}': {error}", dto.Name, createdResult.Message ?? "unknown error");
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = $"An internal server error while creating a '{dto.Name}' project" });
            }
            if(!addedResult.Success)
            {
                _logger.LogWarning("Failed to add user {userId} in project {projectId}: {error}", UserId, project.Id, createdResult.Message ?? "unknown error");
            }
            else
            {
                await _appHubContext.Clients.User(UserId).SendAsync(Constants.SOKET_EVENT_PROJECT_CREATED);
            }

            await _appHubContext.Clients.Group(Constants.GROUP_ADMINS).SendAsync(Constants.SOKET_EVENT_PROJECT_CREATED);
            return Ok(new CreateProjectRequest { ProjectId = project.Id });
        }

        [HttpPut("{id}", Name = "UpdateProject")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> UpdateProject(string id, [FromBody] ProjectUpdateRequest dto)
        {
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = dto.OwnerId ?? ""
            };

            var result = await _repository.Update(id, project);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(id).SendAsync(Constants.SOKET_EVENT_PROJECT_UPDATED);
                await _appHubContext.Clients.Group(Constants.GROUP_ADMINS).SendAsync(Constants.SOKET_EVENT_PROJECT_UPDATED, id);
                return NoContent();
            }

            _logger.LogError("Failed to update a project {id}: {error}", id, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = $"An internal server error while updating a project" });
        }

        [HttpDelete("{id}", Name = "DeleteProject")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteProject(string id)
        {
            var result = await _repository.Delete(id);
            if (result.Success)
            {
                await _projectHubContext.Clients.Group(id).SendAsync(Constants.SOKET_EVENT_PROJECT_DELETED);
                await _appHubContext.Clients.Group(Constants.GROUP_ALL).SendAsync(Constants.SOKET_EVENT_PROJECT_DELETED, id);
                return NoContent();
            }

            _logger.LogError("Failed to delete a project {id}: {error}", id, result.Message ?? "unknown error");
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = $"An internal server error while deleting a project" });
        }
    }
}
