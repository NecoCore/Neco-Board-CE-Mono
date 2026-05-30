using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;
using neco_board_ce.Repositories.Tables;

namespace neco_board_ce.Utils.Check
{
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

        public async Task<ChekResult> HasAccessToColumn(string userId, string columnId)
        {
            var column = await _columnsRepository.GetById(columnId);
            if(!column.Success)
            {
                return new ChekResult { Result = false, Message = column.Message };
            }
            if(column.Data is null) return new ChekResult { Result = false, Message = "Column not found" };

            var projectId = column.Data.ProjectId;

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (userInProject.Success) return new ChekResult { Result = false, Message = userInProject.Message };
            if (userInProject.Data is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };

            return new ChekResult { Result = true, ProjectId = column.Data.ProjectId };
        }

        public async Task<ChekResult> HasAccessToColumn(string userId, string columnId, ProjectRole role)
        {
            var column = await _columnsRepository.GetById(columnId);
            if (!column.Success)
            {
                return new ChekResult { Result = false, Message = column.Message };
            }
            if (column.Data is null) return new ChekResult { Result = false, Message = "Column not found" };

            var projectId = column.Data.ProjectId;

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (userInProject.Success) return new ChekResult { Result = false, Message = userInProject.Message };
            if (userInProject.Data is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };
            if (userInProject.Data.Role > role) return new ChekResult { Result = false, Message = "You don't have access to this route" };

            return new ChekResult { Result = true, ProjectId = column.Data.ProjectId };
        }

        public async Task<ChekResult> HasAccessToTask(string userId, string taskId)
        {
            var projectId = await _taskRepository.GetProjectById(taskId);
            if(!projectId.Success) return new ChekResult { Result = false, Message = projectId.Message };
            if (projectId.Data is null) return new ChekResult { Result = false, Message = "Task not found" };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId.Data);
            if (userInProject.Success) return new ChekResult { Result = false, Message = userInProject.Message };
            if (userInProject.Data is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };

            return new ChekResult { Result = true, ProjectId = projectId.Data };
        }

        public async Task<ChekResult> HasAccessToTask(string userId, string taskId, ProjectRole role)
        {
            var projectId = await _taskRepository.GetProjectById(taskId);
            if (!projectId.Success) return new ChekResult { Result = false, Message = projectId.Message };
            if (projectId.Data is null) return new ChekResult { Result = false, Message = "Task not found" };

            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId.Data);
            if (userInProject.Success) return new ChekResult { Result = false, Message = userInProject.Message };
            if (userInProject.Data is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };
            if (userInProject.Data.Role > role) return new ChekResult { Result = false, Message = "You don't have access to this route" };

            return new ChekResult { Result = true, ProjectId = projectId.Data };
        }

        public async Task<ChekResult> HasAccessToProject(string userId, string projectId)
        {
            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if(userInProject.Success) return new ChekResult { Result = false, Message = userInProject.Message };
            if (userInProject.Data is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };

            return new ChekResult { Result = true };
        }

        public async Task<ChekResult> HasAccessToProject(string userId, string projectId, ProjectRole role)
        {
            var userInProject = await _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (userInProject.Success) return new ChekResult { Result = false, Message = userInProject.Message };
            if (userInProject.Data is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };
            if (userInProject.Data.Role > role) return new ChekResult { Result = false, Message = "You don't have access to this route" };

            return new ChekResult { Result = true };
        }
    }
}
