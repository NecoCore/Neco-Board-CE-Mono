using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;

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

        public async Task<List<TaskUser>> GetByTaskId(string taskId)
        {
            _logger.LogDebug("Fetching users for task ID: {TaskId} from the database.", taskId);
            return await _db.TaskUsers
                .Include(tu => tu.User)
                .Where(tu => tu.TaskId == taskId)
                .ToListAsync();
        }

        public async Task<List<TaskUser>> GetByUserId(string userId)
        {
            _logger.LogDebug("Fetching tasks for user ID: {UserId} from the database.", userId);
            return await _db.TaskUsers
                .Include(tu => tu.Task)
                .Where(tu => tu.UserId == userId)
                .ToListAsync();
        }

        public async Task<List<TaskUser>> GetFullByUserId(string userId)
        {
            _logger.LogDebug("Fetching tasks for user ID: {UserId} from the database.", userId);
            return await _db.TaskUsers
                .Include(tu => tu.Task)
                    .ThenInclude(t => t.Column)
                        .ThenInclude(c => c.Project)
                .Where(tu => tu.UserId == userId)
                .ToListAsync();
        }

        public async Task<bool> AddUser(string taskId, string userId)
        {
            _logger.LogDebug("Adding user ID: {UserId} to task ID: {TaskId} in the database.", userId, taskId);

            var alreadyAssigned = await _db.TaskUsers.AnyAsync(tu => tu.TaskId == taskId && tu.UserId == userId);
            if (alreadyAssigned)
            {
                _logger.LogWarning("User ID: {UserId} is already assigned to task ID: {TaskId}.", userId, taskId);
                return false;
            }

            var taskExists = await _db.ColumnTasks.AnyAsync(t => t.Id == taskId);
            if (!taskExists)
            {
                _logger.LogWarning("Task with ID: {TaskId} not found in the database.", taskId);
                return false;
            }

            var userExists = await _db.Accounts.AnyAsync(a => a.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Account with ID: {UserId} not found in the database.", userId);
                return false;
            }

            await _db.TaskUsers.AddAsync(new TaskUser { TaskId = taskId, UserId = userId });
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveUser(string taskId, string userId)
        {
            _logger.LogDebug("Removing user ID: {UserId} from task ID: {TaskId} in the database.", userId, taskId);

            var existing = await _db.TaskUsers.FirstOrDefaultAsync(tu => tu.TaskId == taskId && tu.UserId == userId);
            if (existing is null)
            {
                _logger.LogWarning("Assignment of user ID: {UserId} to task ID: {TaskId} not found.", userId, taskId);
                return false;
            }

            _db.TaskUsers.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
