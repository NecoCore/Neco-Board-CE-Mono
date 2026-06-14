using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    public class ColumnTaskRepository : ICRUDRepository<ColumnTask>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ColumnTaskRepository> _logger;

        public ColumnTaskRepository(AppDbContext db, ILogger<ColumnTaskRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RepositoryResult<List<ColumnTask>>> GetAll()
        {
            _logger.LogDebug("Fetching all tasks from the database.");
            try
            {
                var tasks = await _db.ColumnTasks.Include(t => t.Owner).ToListAsync();
                return new RepositoryResult<List<ColumnTask>> { Success = true, Data = tasks };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching tasks from the database.");
                return new RepositoryResult<List<ColumnTask>> { Success = false, Message = "An error occurred while fetching tasks." };
            }
        }

        public async Task<RepositoryResult<ColumnTask?>> GetById(Guid id)
        {
            _logger.LogDebug("Fetching task with ID: {Id} from the database.", id);
            try
            {
                var task = await _db.ColumnTasks
                    .Include(t => t.Owner)
                    .Include(t => t.Users).ThenInclude(u => u.User)
                    .Include(t => t.Images)
                    .Include(t => t.Attachments)
                    .AsSplitQuery()
                    .FirstOrDefaultAsync(t => t.Id == id);
                return new RepositoryResult<ColumnTask?> { Success = true, Data = task };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task with ID: {Id} from the database.", id);
                return new RepositoryResult<ColumnTask?> { Success = false, Message = "An error occurred while fetching task." };
            }
        }

        public async Task<RepositoryResult<Guid?>> GetProjectById(Guid id)
        {
            _logger.LogDebug("Fetching project ID for task { id }.", id);
            try
            {
                var projectId = await _db.ColumnTasks
                    .Where(t => t.Id == id)
                    .Select(t => t.Column.ProjectId)
                    .FirstOrDefaultAsync();
                return new RepositoryResult<Guid?> { Success = true, Data = projectId == Guid.Empty ? null : projectId };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching project ID for task: {Id} from the database.", id);
                return new RepositoryResult<Guid?> { Success = false, Message = "An error occurred while fetching project ID." };
            }
        }

        public async Task<RepositoryResult<List<ColumnTask>>> GetByColumnId(Guid columnId)
        {
            _logger.LogDebug("Fetching tasks for column ID: {ColumnId} from the database.", columnId);
            try
            {
                var tasks = await _db.ColumnTasks
                    .Include(t => t.Owner)
                    .Where(t => t.ColumnId == columnId)
                    .ToListAsync();
                return new RepositoryResult<List<ColumnTask>> { Success = true, Data = tasks };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching tasks for column ID: {ColumnId} from the database.", columnId);
                return new RepositoryResult<List<ColumnTask>> { Success = false, Message = "An error occurred while fetching tasks." };
            }
        }

        public async Task<RepositoryResult<bool>> Create(ColumnTask entity)
        {
            _logger.LogDebug("Creating a new task with name: {Name} in the database.", entity.Name);
            await _db.ColumnTasks.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create task." };
        }

        public async Task<RepositoryResult<bool>> Update(Guid id, ColumnTask entity)
        {
            _logger.LogDebug("Updating task with ID: {Id} in the database.", id);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task not found." };
            }

            existing.Name = entity.Name;
            existing.Description = entity.Description;
            existing.Text = entity.Text;

            _db.ColumnTasks.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update task." };
        }

        public async Task<RepositoryResult<Guid>> UpdateStatus(Guid id, ColumnTaskStatus status)
        {
            _logger.LogDebug("Updating status of task with ID: {Id} to {Status} in the database.", id, status);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return new RepositoryResult<Guid> { Success = false, Message = "Task not found." };
            }

            existing.Status = status;
            _db.ColumnTasks.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<Guid> { Success = saved, Message = saved ? string.Empty : "Failed to update task status.", Data = existing.ColumnId };
        }

        public async Task<RepositoryResult<Guid>> UpdatePriority(Guid id, TaskPriority priority)
        {
            _logger.LogDebug("Updating priority of task with ID: {Id} to {Priority} in the database.", id, priority);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return new RepositoryResult<Guid> { Success = false, Message = "Task not found." };
            }

            existing.Priority = priority;
            _db.ColumnTasks.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<Guid> { Success = saved, Message = saved ? string.Empty : "Failed to update task priority.", Data = existing.ColumnId };
        }

        public async Task<RepositoryResult<bool>> MoveToColumn(Guid id, Guid columnId)
        {
            _logger.LogDebug("Moving task with ID: {Id} to column ID: {ColumnId} in the database.", id, columnId);
            var existing = await _db.ColumnTasks.Where(t => t.Id == id).Include(t => t.Column).FirstOrDefaultAsync();
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task not found." };
            }
            var oldColumnId = existing.ColumnId;

            var columnExists = await _db.Columns.AnyAsync(c => c.ProjectId == existing.Column.ProjectId && c.Id == columnId);
            if (!columnExists)
            {
                _logger.LogWarning("Column with ID: {ColumnId} not found in the database.", columnId);
                return new RepositoryResult<bool> { Success = false, Message = "Column not found." };
            }

            existing.ColumnId = columnId;
            _db.ColumnTasks.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? oldColumnId.ToString() : "Failed to move task to column." };
        }

        public async Task<RepositoryResult<bool>> Delete(Guid id)
        {
            _logger.LogDebug("Deleting task with ID: {Id} from the database.", id);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task not found." };
            }

            var colId = existing.ColumnId;
            _db.ColumnTasks.Remove(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? colId.ToString() : "Failed to delete task." };
        }
    }
}
