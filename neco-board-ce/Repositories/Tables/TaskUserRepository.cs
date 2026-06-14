using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    public class TaskUserRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TaskUserRepository> _logger;

        public TaskUserRepository(AppDbContext db, ILogger<TaskUserRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RepositoryResult<List<TaskUser>>> GetByTaskId(Guid taskId)
        {
            _logger.LogDebug("Fetching users for task ID: {TaskId} from the database.", taskId);
            try
            {
                var taskUsers = await _db.TaskUsers
                    .Include(tu => tu.User)
                    .Where(tu => tu.TaskId == taskId)
                    .ToListAsync();
                return new RepositoryResult<List<TaskUser>> { Success = true, Data = taskUsers };
            } 
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error to get users in task: {TaskId}", taskId);
                return new RepositoryResult<List<TaskUser>>
                {
                    Success = false,
                    Message = "An error occurred while fetching users in task."
                };
            }
        }

        public async Task<RepositoryResult<List<TaskUser>>> GetByUserId(Guid userId)
        {
            _logger.LogDebug("Fetching tasks for user ID: {UserId} from the database.", userId);
            try
            {
                var taskUsers = await _db.TaskUsers
                    .Include(tu => tu.Task)
                    .Where(tu => tu.UserId == userId)
                    .ToListAsync();
                return new RepositoryResult<List<TaskUser>> { Success = true, Data = taskUsers };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error to get tasks for user: {UserId}", userId);
                return new RepositoryResult<List<TaskUser>>
                {
                    Success = false,
                    Message = "An error occurred while fetching tasks for user."
                };
            }
        }

        public async Task<RepositoryResult<List<TaskUser>>> GetFullByUserId(Guid userId)
        {
            _logger.LogDebug("Fetching tasks for user ID: {UserId} from the database.", userId);
            try
            {
                var taskUsers = await _db.TaskUsers
                    .Include(tu => tu.Task)
                        .ThenInclude(t => t.Column)
                            .ThenInclude(c => c.Project)
                    .Where(tu => tu.UserId == userId)
                    .AsSplitQuery()
                    .ToListAsync();
                return new RepositoryResult<List<TaskUser>> { Success = true, Data = taskUsers };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error to get full information for user: {UserId}", userId);
                return new RepositoryResult<List<TaskUser>>
                {
                    Success = false,
                    Message = "An error occurred while fetching full information for user."
                };
            }
        }

        public async Task<RepositoryResult<bool>> AddUser(Guid taskId, Guid userId)
        {
            _logger.LogDebug("Adding user ID: {UserId} to task ID: {TaskId} in the database.", userId, taskId);

            var alreadyAssigned = await _db.TaskUsers.AnyAsync(tu => tu.TaskId == taskId && tu.UserId == userId);
            if (alreadyAssigned)
            {
                _logger.LogWarning("User ID: {UserId} is already assigned to task ID: {TaskId}.", userId, taskId);
                return new RepositoryResult<bool> { Success = false, Message = "User is already assigned to this task." };
            }

            var taskExists = await _db.ColumnTasks.AnyAsync(t => t.Id == taskId);
            if (!taskExists)
            {
                _logger.LogWarning("Task with ID: {TaskId} not found in the database.", taskId);
                return new RepositoryResult<bool> { Success = false, Message = "Task not found." };
            }

            var userExists = await _db.Accounts.AnyAsync(a => a.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Account with ID: {UserId} not found in the database.", userId);
                return new RepositoryResult<bool> { Success = false, Message = "User not found." };
            }

            await _db.TaskUsers.AddAsync(new TaskUser { TaskId = taskId, UserId = userId });
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to add user." };
        }

        public async Task<RepositoryResult<bool>> RemoveUser(Guid taskId, Guid userId)
        {
            _logger.LogDebug("Removing user ID: {UserId} from task ID: {TaskId} in the database.", userId, taskId);

            var existing = await _db.TaskUsers.FirstOrDefaultAsync(tu => tu.TaskId == taskId && tu.UserId == userId);
            if (existing is null)
            {
                _logger.LogWarning("Assignment of user ID: {UserId} to task ID: {TaskId} not found.", userId, taskId);
                return new RepositoryResult<bool> { Success = false, Message = "Assignment not found." };
            }

            _db.TaskUsers.Remove(existing);
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to remove user." };
        }
    }
}
