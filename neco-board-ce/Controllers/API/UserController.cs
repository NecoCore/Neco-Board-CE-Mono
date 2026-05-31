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
    [ApiController]
    [Route("api/users")]
    [Authorize]
    public class UserController : UserAuth
    {
        private readonly ILogger<UserController> _logger;
        private readonly AccountRepository _repository;
        private readonly UserProjectRoleRepository _userProjectReposirory;
        private readonly ProjectRepository _projectRepository;
        private readonly TaskUserRepository _taskUserRepository;
        private readonly UserAccessCheck _userAccess;

        public UserController(
            ILogger<UserController> logger, 
            AccountRepository repository, 
            UserProjectRoleRepository userProjectRepository, 
            ProjectRepository projectRepository, 
            TaskUserRepository taskUserRepository,
            UserAccessCheck userAccess
            )
        {
            _logger = logger;
            _repository = repository;
            _userProjectReposirory = userProjectRepository;
            _projectRepository = projectRepository;
            _taskUserRepository = taskUserRepository;
            _userAccess = userAccess;
        }

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
                _logger.LogError("Error to get all users: {Error}", resut.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Server error. Try letter" });
            }
            else if (resut.Data is null) return NoContent();

            var data = resut.Data.Select(x => new UserInfoResponse(x)).ToList();
            return Ok(data);
        }

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
                _logger.LogError("Error to get all users for admins: {Error}", resut.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Server error. Try letter" });
            }
            else if (resut.Data is null) return NoContent();
            return Ok(resut.Data);
        }

        [HttpPatch("role/{id}", Name = "EdirUserRole")]
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
                _logger.LogWarning("Error to edit user {UserId} role: {Error}", id, result.Message);
                return BadRequest(new ErrorMessageResponse { Message = result.Message ?? "Forbid" });
            }

            return NoContent();
        }

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
                _logger.LogError("Error to get all users for admins: {Error}", result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Server error. Try letter" });
            }
            if (result.Data is null) return NotFound(new ErrorMessageResponse { Message = "User not found" });
            var data = result.Data;
            if(BCrypt.Net.BCrypt.Verify(dto.OldPassword, data.Password)) return BadRequest(new ErrorMessageResponse { Message = "Wrong password" });
            if(dto.Password != dto.ConfirmPassword) return BadRequest(new ErrorMessageResponse { Message = "New password do not match" });

            var updateResult = await _repository.UpdatePassword(UserId!, dto.Password);
            if(updateResult.Success)
            {
                return NoContent();
            }

            _logger.LogError("Error to get all users for admins: {Error}", result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Server error. Try letter" });
        }

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
                _logger.LogError("Error to get all users for admins: {Error}", result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Server error. Try letter" });
            }
            var data = result.Data?.Select(p => new ProjectItemResponse(p)).ToList();
            return data is null ? NoContent() : Ok(data);
        }

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
                _logger.LogError("Error to get all users for admins: {Error}", result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Server error. Try letter" });
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

        [HttpDelete("{userId}", Name = "DeleteUser")]
        [Authorize(Roles = "ADMIN,OWNER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ErrorMessageResponse), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteUser(string userId)
        {
            if (UserId == userId) return BadRequest(new ErrorMessageResponse { Message = "You cannot delete yourself." });
            var result = await _repository.GetById(userId);
            if(!result.Success)
            {
                _logger.LogError("Error to get all users for admins: {Error}", result.Message);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new ErrorMessageResponse { Message = "Server error. Try letter" });
            }
            if (result.Data is null) return NotFound(new ErrorMessageResponse { Message = "User not found." });
            if (result.Data.Role == Models.Enums.WorkspaceRoles.OWNER) return Forbid();
            if (result.Data.Role == Models.Enums.WorkspaceRoles.ADMIN && !IsWorkspaceOwner()) return Forbid();
            var deleteResult = await _repository.Delete(userId);
            if(deleteResult.Success)
            {
                return NoContent();
            }

            _logger.LogError("Error to get all users for admins: {Error}", result.Message);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new ErrorMessageResponse { Message = "Server error. Try letter" });
        }
    }
}
