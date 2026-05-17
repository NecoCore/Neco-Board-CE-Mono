using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request;
using neco_board_ce.Models.DTO.Request.Tasks;
using neco_board_ce.Models.DTO.Response.Task;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Utils.Controllers;

namespace neco_board_ce.Controllers.API
{
    [ApiController]
    [Authorize]
    [Route("api/project/{projectId}/column/{columnId}/task")]
    public class TaskColumnController : UserAuth
    {
        private readonly ILogger<TaskColumnController> _logger;
        private readonly ColumnTaskRepository _repository;
        private readonly UserProjectRoleRepository _userProjectReposirory;
        private readonly TaskUserRepository _taskUserRepository;
        private readonly IHubContext<ProjectHub> _projectHubContext;
        private readonly IHubContext<TaskHub> _taskHubContext;

        public TaskColumnController(ILogger<TaskColumnController> logger, ColumnTaskRepository repository, UserProjectRoleRepository userProjectReposirory, TaskUserRepository taskUserRepository, IHubContext<ProjectHub> projectHubConext, IHubContext<TaskHub> taskHubContext)
        {
            _logger = logger;
            _repository = repository;
            _userProjectReposirory = userProjectReposirory;
            _taskUserRepository = taskUserRepository;
            _projectHubContext = projectHubConext;
            _taskHubContext = taskHubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetInColumn(string projectId, string columnId)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();
            }

            var tasks = await _repository.GetByColumnId(columnId);
            if (tasks is null) return NoContent();

            var taskLitle = tasks.Select(t => new
            {
                t.Id,
                t.Name,
                t.Description,
                t.Priority,
                t.Status,
                t.CreatedAt,
                t.ColumnId
            }).ToList();

            return Ok(taskLitle);
        }

        [HttpGet("{taskId}")]
        public async Task<IActionResult> GetTaskInfo(string projectId, string taskId)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();
            }

            var task = await _repository.GetById(taskId);
            if (task is null) return NoContent();
            return Ok(task);
        }

        [HttpPost]
        public async Task<IActionResult> Create(string projectId, string columnId, [FromBody] TaskColumnRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER) return Forbid();
            }

            var task = new ColumnTask
            {
                ColumnId = columnId,
                OwnerId = UserId,
                Name = dto.Name,
                Description = dto.Description,
                Text = dto.Text,
            };
            var result = await _repository.Create(task);

            if(result) 
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_TASK_CREATED, columnId);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPut("{taskId}")]
        public async Task<IActionResult> Update(string projectId, string taskId, [FromBody] TaskColumnRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER) return Forbid();
            }

            var task = new ColumnTask
            {
                OwnerId = UserId,
                Name = dto.Name,
                Description = dto.Description,
                Text = dto.Text,
            };
            var result = await _repository.Update(taskId, task);

            if (result)
            {
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_TASK_UPDATED, taskId);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPatch("{taskId}/status")]
        public async Task<IActionResult> UpdateStatus(string projectId, string columnId, string taskId, [FromBody] EditTaskStatusRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER) return Forbid();
            }

            var result = await _repository.UpdateStatus(taskId, dto.Status);

            if (result)
            {
                await _taskHubContext.Clients.Group(taskId).SendAsync(Constants.SOKET_EVENT_TASK_STATUS_UPDATED);
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_TASK_STATUS_UPDATED, columnId);
                return Ok();
            }
            return BadRequest();
        }

        [HttpPatch("{taskId}/priority")]
        public async Task<IActionResult> UpdatePriority(string projectId, string columnId, string taskId, [FromBody] EditTaskPriorityRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER) return Forbid();
            }

            var result = await _repository.UpdatePriority(taskId, dto.Priority);

            if (result)
            {
                await _taskHubContext.Clients.Group(taskId).SendAsync(Constants.SOKET_EVENT_TASK_PRIORITY_UPDATED);
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_TASK_PRIORITY_UPDATED, new EditTaskSocketResponse
                {
                    TaskId = taskId,
                    ColumnId = columnId
                });
                return Ok();
            }
            return BadRequest();
        }

        [HttpPatch("{taskId}/column")]
        public async Task<IActionResult> UpdateColumn(string projectId, string taskId, string columnId, [FromBody] EditTaskColumnRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER) return Forbid();
            }

            var result = await _repository.MoveToColumn(taskId, dto.ColumnId);

            if (result)
            {
                var response = new NewTaskColumnSocketRequest
                {
                    OldColumnId = columnId,
                    NewColumnId = dto.ColumnId
                };
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_TASK_COLUMN_UPDATED, response);
                return Ok();
            }
            return BadRequest();
        }

        [HttpGet("{taskId}/user")]
        public async Task<IActionResult> GetUsers(string projectId, string taskId)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null) return Forbid();
            }

            var users = await _taskUserRepository.GetByTaskId(taskId);
            return Ok(users);
        }

        [HttpPost("{taskId}/user")]
        public async Task<IActionResult> AddUser(string projectId, string taskId, [FromBody] AddUserInTaskRequest dto)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);

                if (user is null || user.Role == ProjectRole.VIEWER)
                    return Forbid();

                if (user.Role == ProjectRole.USER && dto.UserId is not null)
                    return Forbid();
            }

            string targetUserId = dto.UserId ?? UserId;
            var result = await _taskUserRepository.AddUser(taskId, targetUserId);

            if (result)
            {
                await _taskHubContext.Clients.Group(taskId).SendAsync(Constants.SOKET_EVENT_TASK_USER_ADDED);
                return Ok();
            }
            return BadRequest();
        }

        [HttpDelete("{taskId}/user")]
        public async Task<IActionResult> RemoveUser(string projectId, string taskId, [FromBody] string userId)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER)
                    return Forbid();
                if (user.Role == ProjectRole.USER && userId != UserId)
                    return Forbid();
            }
            var result = await _taskUserRepository.RemoveUser(taskId, userId);

            if (result)
            {
                await _taskHubContext.Clients.Group(taskId).SendAsync(Constants.SOKET_EVENT_TASK_USER_REMOVED);
                return Ok();
            }
            return BadRequest();
        }

        [HttpDelete("{taskId}")]
        public async Task<IActionResult> DeleteTask(string projectId, string taskId)
        {
            if (!IsWorkspaceAdmin())
            {
                var user = await _userProjectReposirory.GetByUserAndProject(UserId, projectId);
                if (user is null || user.Role == ProjectRole.VIEWER) return Forbid();
            }

            var result = await _repository.Delete(taskId);

            if (result)
            {
                await _taskHubContext.Clients.Group(taskId).SendAsync(Constants.SOKET_EVENT_TASK_DELETED);
                await _projectHubContext.Clients.Group(projectId).SendAsync(Constants.SOKET_EVENT_TASK_PRIORITY_UPDATED, taskId);
                return Ok();
            }
            return BadRequest();
        }
    }
}
    

