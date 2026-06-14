using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;
using neco_board_ce.Repositories.Tables;

namespace neco_board_ce.Utils.Check
{
    /// <summary>
    /// Provides methods for checking user access permissions to projects, columns, and tasks.
    /// Supports both simple membership checks and role-based authorization.
    /// </summary>
    public class UserAccessCheck
    {
        private readonly ColumnsRepository _columnsRepository;
        private readonly ColumnTaskRepository _taskRepository;
        private readonly UserProjectRoleRepository _userProjectRoleRepository;
        private readonly AccountRepository _accountRepository;

        public UserAccessCheck(
            ColumnsRepository columnsRepository, 
            ColumnTaskRepository taskRepository, 
            UserProjectRoleRepository userProjectRoleRepository,
            AccountRepository accountRepository)
        {
            _columnsRepository = columnsRepository;
            _taskRepository = taskRepository;
            _userProjectRoleRepository = userProjectRoleRepository;
            _accountRepository = accountRepository;
        }

        private async Task<bool> IsGlobalAdmin(Guid userId)
        {
            var user = await _accountRepository.GetById(userId);
            if (!user.Success || user.Data is null) return false;
            return user.Data.Role == WorkspaceRoles.ADMIN || user.Data.Role == WorkspaceRoles.OWNER;
        }

        /// <summary>
        /// Checks if a user has basic access to a column (is a member of the parent project or global admin).
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="columnId">The ID of the column.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID if found.</returns>
        public async Task<CheckResult> HasAccessToColumn(Guid userId, Guid columnId)
        {
            var column = await _columnsRepository.GetById(columnId);
            if(!column.Success)
            {
                return new CheckResult { Result = false, Message = column.Message };
            }
            if(column.Data is null) return new CheckResult { Result = false, Message = "Column not found" };

            var projectId = column.Data.ProjectId;

            if (await IsGlobalAdmin(userId)) return new CheckResult { Result = true, ProjectId = projectId };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has access to a column with a minimum required project role.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="columnId">The ID of the column.</param>
        /// <param name="role">The minimum required role (e.g., MODERATOR).</param>
        /// <returns>A <see cref="CheckResult"/> with <c>Result = true</c> only if the user has the required role or higher.</returns>
        public async Task<CheckResult> HasAccessToColumn(Guid userId, Guid columnId, ProjectRole role)
        {
            var column = await _columnsRepository.GetById(columnId);
            if (!column.Success)
            {
                return new CheckResult { Result = false, Message = column.Message };
            }
            if (column.Data is null) return new CheckResult { Result = false, Message = "Column not found" };

            var projectId = column.Data.ProjectId;

            if (await IsGlobalAdmin(userId)) return new CheckResult { Result = true, ProjectId = projectId };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };
            
            // Weight check: lower index means higher priority (OWNER=0, MODERATOR=1, USER=2, VIEWER=3)
            if (userInProject.Data.Role > role) return new CheckResult { Result = false, Message = "You don't have access to this route", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has basic access to a task (is a member of the parent project or global admin).
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="taskId">The ID of the task.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID if found.</returns>
        public async Task<CheckResult> HasAccessToTask(Guid userId, Guid taskId)
        {
            var projectIdResult = await _taskRepository.GetProjectById(taskId);
            if(!projectIdResult.Success) return new CheckResult { Result = false, Message = projectIdResult.Message };
            if (projectIdResult.Data is null) return new CheckResult { Result = false, Message = "Task not found" };

            var projectId = projectIdResult.Data.Value;

            if (await IsGlobalAdmin(userId)) return new CheckResult { Result = true, ProjectId = projectId };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has access to a task with a minimum required project role.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="taskId">The ID of the task.</param>
        /// <param name="role">The minimum required role.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID.</returns>
        public async Task<CheckResult> HasAccessToTask(Guid userId, Guid taskId, ProjectRole role)
        {
            var projectIdResult = await _taskRepository.GetProjectById(taskId);
            if (!projectIdResult.Success) return new CheckResult { Result = false, Message = projectIdResult.Message };
            if (projectIdResult.Data is null) return new CheckResult { Result = false, Message = "Task not found" };

            var projectId = projectIdResult.Data.Value;

            if (await IsGlobalAdmin(userId)) return new CheckResult { Result = true, ProjectId = projectId };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };
            if (userInProject.Data.Role > role) return new CheckResult { Result = false, Message = "You don't have access to this route", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has basic membership in a project (or is global admin).
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID.</returns>
        public async Task<CheckResult> HasAccessToProject(Guid userId, Guid projectId)
        {
            if (await IsGlobalAdmin(userId)) return new CheckResult { Result = true, ProjectId = projectId };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if(!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has a minimum required role in a project.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="projectId">The ID of the project.</param>
        /// <param name="role">The minimum required role.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID.</returns>
        public async Task<CheckResult> HasAccessToProject(Guid userId, Guid projectId, ProjectRole role)
        {
            if (await IsGlobalAdmin(userId)) return new CheckResult { Result = true, ProjectId = projectId };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };
            if (userInProject.Data.Role > role) return new CheckResult { Result = false, Message = "You don't have access to this route", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }
    }
}
