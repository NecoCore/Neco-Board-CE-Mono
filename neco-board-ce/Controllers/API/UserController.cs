using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Controllers;
using System.Security.Claims;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserController : UserAuth
    {
        private readonly ILogger<UserController> _logger;
        private readonly AccountRepository _repository;
        private readonly UserProjectRoleRepository _userProjectReposirory;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskUserRepository _taskUserRepository;

        public UserController(ILogger<UserController> logger, AccountRepository repository, UserProjectRoleRepository userProjectRepository, ProjectRepository projectRepository, TaskUserRepository taskUserRepository)
        {
            _logger = logger;
            _repository = repository;
            _userProjectReposirory = userProjectRepository;
            _projectRepository = projectRepository;
            _taskUserRepository = taskUserRepository;
        }

        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var users = await _repository.GetAll();

            var litleUsers = users.Select(x => new
            {
                x.Id,
                x.Name,
                x.Avatar,
                x.Role
            }).ToList();

            return Ok(litleUsers);
        }

        [HttpGet("all")]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> GetAllFull()
        {
            var users = await _repository.GetAll();
            return Ok(users);
        }

        [HttpPatch("role/{id}")]
        [Authorize(Roles = "OWNER")]
        public async Task<IActionResult> EditRole(string id, [FromBody] EditWorkspaceRoleRequest dto)
        {
            if (dto.Role == Models.Enums.WorkspaceRoles.OWNER)
                return Forbid("There can only be one owner");

            var result = await _repository.UpdateRole(id, dto.Role);

            if (result)
                return Ok(new { massage = "Role has been change" });
            else
                return BadRequest();
        }

        [HttpPatch("updatePassword")]
        [Authorize]
        public async Task<IActionResult> EditPassword(EditPasswordRequest dto)
        {
            if (UserId is null) return Unauthorized();

            var user = await _repository.GetById(UserId);
            if(user == null) return BadRequest();
            if(BCrypt.Net.BCrypt.Verify(dto.OldPassword, user.Password)) return BadRequest("Wrong password");
            if(dto.Password != dto.ConfirmPassword) return BadRequest("New password do not match");

            var result = await _repository.UpdatePassword(UserId, dto.Password);
            return result ? Ok() : BadRequest();
        }

        [HttpGet("projects")]
        [Authorize]
        public async Task<IActionResult> GetMyProjects()
        {
            if(UserId is null) return Unauthorized();
            var projects = await _projectRepository.GetAllByUserId(UserId);
            return Ok(projects);
        }

        [HttpGet("tasks")]
        [Authorize]
        public async Task<IActionResult> GetMyTasks()
        {
            var myTasks = await _taskUserRepository.GetFullByUserId(UserId);
            var tasks = myTasks
                .Select(t => new
                {
                    Id = t.Task.Id,
                    Name = t.Task.Name,
                    Description = t.Task.Description,
                    Status = t.Task.Status,
                    Priority = t.Task.Priority,
                    CreatedAt = t.Task.CreatedAt,
                    ColumnId = t.Task.ColumnId,
                    Project = t.Task.Column?.Project?.Name ?? "Unknown Project",
                    ProjectId = t.Task.Column?.ProjectId ?? null
                })
                .GroupBy(x => x.Project)
                .Select(group => new
                {
                    ProjectName = group.Key,
                    ProjectId = group.First().ProjectId,
                    Tasks = group.Select(task => new
                    {
                        task.Id,
                        task.Name,
                        task.Description,
                        task.Status,
                        task.Priority,
                        task.CreatedAt,
                        task.ColumnId
                    }),
                })
                .ToList();

            return Ok(tasks);
        }

        [HttpDelete("{userId}")]
        [Authorize(Roles = "ADMIN,OWNER")]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (UserId == userId) return BadRequest("You cannot delete yourself.");
            var userInfo = await _repository.GetById(userId);
            if (userInfo == null) return NotFound("User with ID {userId} not found.");
            if (userInfo.Role == Models.Enums.WorkspaceRoles.OWNER) return Forbid("You can delete the Owner");
            if (userInfo.Role == Models.Enums.WorkspaceRoles.ADMIN && !IsWorkspaceOwner()) return Forbid("You don't have permission to delete this user");
            var result = await _repository.Delete(userId);
            return result ? Ok() : BadRequest();
        }
    }
}
