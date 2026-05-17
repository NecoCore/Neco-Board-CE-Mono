using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using neco_board_ce.Models.Entity;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Controllers;
using neco_board_ce.Models.DTO.Request;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;

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

        public ProjectController(ILogger<ProjectController> logger, ProjectRepository repository, UserProjectRoleRepository userProject, IHubContext<AppHub> appHubContext, IHubContext<ProjectHub> projectHubContext)
        {
            _logger = logger;
            _repository = repository;
            _userProjectReposirory = userProject;
            _appHubContext = appHubContext;
            _projectHubContext = projectHubContext;
        }

        [HttpGet]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> GetAllProjects()
        {
            var projects = await _repository.GetAll();
            return Ok(projects);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetProjectById(string id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var users = await _userProjectReposirory.GetByProjectId(id);
            var existing = users.Select(u => u.UserId).Contains(userId);
            if (!existing && !IsWorkspaceAdmin()) return Forbid();

            var project = await _repository.GetById(id);
            if (project == null)
            {
                return NotFound();
            }
            return Ok(project);
        }

        [HttpPost]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> CreateProject([FromBody] ProjectRequest dto)
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (UserId is null)
                return Unauthorized();

            var project = new Project
            {
                OwnerId = UserId,
                Name = dto.Name,
                Description = dto.Description,
            };

            var createdProject = await _repository.Create(project);
            var addadUser = await _userProjectReposirory.AddToProject(UserId, project.Id, Models.Enums.ProjectRole.OWNER);

            if (createdProject && addadUser)
            {
                await _appHubContext.Clients.User(UserId).SendAsync(Constants.SOKET_EVENT_PROJECT_CREATED);
                await _appHubContext.Clients.Group(Constants.GROUP_ADMINS).SendAsync(Constants.SOKET_EVENT_PROJECT_CREATED);
                return Ok(new { projectId = project.Id });
            }
            else if (createdProject)
            {
                _logger.LogWarning("couldn't add the user with the ID {userId} as the owner of the project {pprojectId} to the list of users", UserId, project.Id);
                return Ok(new { projectId = project.Id });
            }
            else
                return BadRequest();
        }

        [HttpPut("{id}")]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> UpdateProject(string id, [FromBody] ProjectUpdateRequest dto)
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (UserId is null)
                return Unauthorized();

            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                OwnerId = dto.OwnerId ?? ""
            };

            var updatedProject = await _repository.Update(id, project);
            if (updatedProject)
            {
                await _projectHubContext.Clients.Group(id).SendAsync(Constants.SOKET_EVENT_PROJECT_UPDATED);
                await _appHubContext.Clients.Group(Constants.GROUP_ADMINS).SendAsync(Constants.SOKET_EVENT_PROJECT_UPDATED, id);
                return Ok();
            }
            else
                return BadRequest();
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> DeleteProject(string id)
        {
            var UserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (UserId is null)
                return Unauthorized();

            var UserRole = User.FindFirstValue(ClaimTypes.Role);

            var deletedProject = await _repository.Delete(id);
            if (deletedProject)
            {
                await _projectHubContext.Clients.Group(id).SendAsync(Constants.SOKET_EVENT_PROJECT_DELETED);
                await _appHubContext.Clients.Group(Constants.GROUP_ALL).SendAsync(Constants.SOKET_EVENT_PROJECT_DELETED, id);
                return Ok();
            }
            else
                return BadRequest();
        }
    }
}
