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

        public UserAccessCheck(ColumnsRepository columnsRepository, ColumnTaskRepository taskRepository, UserProjectRoleRepository userProjectRoleRepository)
        {
            _columnsRepository = columnsRepository;
            _taskRepository = taskRepository;
            _userProjectRoleRepository = userProjectRoleRepository;
        }

        /// <summary>
        /// Checks if a user has basic access to a column (is a member of the parent project).
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="columnId">The ID of the column.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID if found.</returns>
        public async Task<CheckResult> HasAccessToColumn(string userId, string columnId)
        {
            var column = await _columnsRepository.GetById(columnId);
            if(!column.Success)
            {
                return new CheckResult { Result = false, Message = column.Message };
            }
            if(column.Data is null) return new CheckResult { Result = false, Message = "Column not found" };

            var projectId = column.Data.ProjectId;

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
        public async Task<CheckResult> HasAccessToColumn(string userId, string columnId, ProjectRole role)
        {
            var column = await _columnsRepository.GetById(columnId);
            if (!column.Success)
            {
                return new CheckResult { Result = false, Message = column.Message };
            }
            if (column.Data is null) return new CheckResult { Result = false, Message = "Column not found" };

            var projectId = column.Data.ProjectId;

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };
            
            // Weight check: lower index means higher priority (OWNER=0, MODERATOR=1, USER=2, VIEWER=3)
            if (userInProject.Data.Role > role) return new CheckResult { Result = false, Message = "You don't have access to this route", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has basic access to a task (is a member of the parent project).
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="taskId">The ID of the task.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID if found.</returns>
        public async Task<CheckResult> HasAccessToTask(string userId, string taskId)
        {
            var projectIdResult = await _taskRepository.GetProjectById(taskId);
            if(!projectIdResult.Success) return new CheckResult { Result = false, Message = projectIdResult.Message };
            if (projectIdResult.Data is null) return new CheckResult { Result = false, Message = "Task not found" };

            var projectId = projectIdResult.Data;

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
        public async Task<CheckResult> HasAccessToTask(string userId, string taskId, ProjectRole role)
        {
            var projectIdResult = await _taskRepository.GetProjectById(taskId);
            if (!projectIdResult.Success) return new CheckResult { Result = false, Message = projectIdResult.Message };
            if (projectIdResult.Data is null) return new CheckResult { Result = false, Message = "Task not found" };

            var projectId = projectIdResult.Data;

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };
            if (userInProject.Data.Role > role) return new CheckResult { Result = false, Message = "You don't have access to this route", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }

        /// <summary>
        /// Checks if a user has basic membership in a project.
        /// </summary>
        /// <param name="userId">The ID of the user to check.</param>
        /// <param name="projectId">The ID of the project.</param>
        /// <returns>A <see cref="CheckResult"/> containing the result and the project ID.</returns>
        public async Task<CheckResult> HasAccessToProject(string userId, string projectId)
        {
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
        public async Task<CheckResult> HasAccessToProject(string userId, string projectId, ProjectRole role)
        {
            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (!userInProject.Success) return new CheckResult { Result = false, Message = userInProject.Message, ProjectId = projectId };
            if (userInProject.Data is null) return new CheckResult { Result = false, Message = "User doesn't have access in project", ProjectId = projectId };
            if (userInProject.Data.Role > role) return new CheckResult { Result = false, Message = "You don't have access to this route", ProjectId = projectId };

            return new CheckResult { Result = true, ProjectId = projectId };
        }
    }
}
