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

        public UserAccessCheck(UserProjectRoleRepository userProjectRoleRepository)
        {
            _userProjectRoleRepository = userProjectRoleRepository;
        }
        public UserAccessCheck(UserProjectRoleRepository userProjectRoleRepository, ColumnTaskRepository taskRepository)
        {
            _userProjectRoleRepository = userProjectRoleRepository;
            _taskRepository = taskRepository;
        }
        public UserAccessCheck(UserProjectRoleRepository userProjectRoleRepository, ColumnsRepository columnsRepository)
        {
            _userProjectRoleRepository = userProjectRoleRepository;
            _columnsRepository = columnsRepository;
        }

        public async Task<ChekResult> HasAccessToColumn(string userId, string columnId)
        {
            var column = await _columnsRepository.GetById(columnId);
            if(column is null) return new ChekResult { Result = false, Message = "Column not found" };

            var projectId = column.ProjectId;
            var userInProject = _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (userInProject is null) return new ChekResult { Result = false, Message = "User dosen't access have in project" };

            return new ChekResult { Result = true };
        }

        public async Task<ChekResult> HasAccessToTask(string userId, string taskId)
        {
            var projectId = await _taskRepository.GetProjectById(taskId);
            if (projectId is null) return new ChekResult { Result = false, Message = "Task not found" };

            var userInProject = _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (userInProject is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };

            return new ChekResult { Result = true };
        }

        public async Task<ChekResult> HasAccessToProject(string userId, string projectId)
        {
            var userInProject = _userProjectRoleRepository.GetByUserAndProject(userId, projectId);
            if (userInProject is null) return new ChekResult { Result = false, Message = "User dosen't have access in project" };

            return new ChekResult { Result = true };
        }
    }
}
