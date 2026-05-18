using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

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

        public async Task<List<ColumnTask>> GetAll()
        {
            _logger.LogDebug("Fetching all tasks from the database.");
            return await _db.ColumnTasks.Include(t => t.Owner).ToListAsync();
        }

        public async Task<ColumnTask?> GetById(string id)
        {
            _logger.LogDebug("Fetching task with ID: {Id} from the database.", id);
            return await _db.ColumnTasks
                .Include(t => t.Owner)
                .Include(t => t.Users).ThenInclude(u => u.User)
                .Include(t => t.Images)
                .Include(t => t.Attachments)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<string?> GetProjectById(string id)
        {
            _logger.LogDebug("Fetching project ID for task { id }.", id);
            return await _db.ColumnTasks
                .Where(t => t.Id == id)
                .Select(t => t.Column.ProjectId)
                .FirstOrDefaultAsync();
        }

        public async Task<List<ColumnTask>> GetByColumnId(string columnId)
        {
            _logger.LogDebug("Fetching tasks for column ID: {ColumnId} from the database.", columnId);
            return await _db.ColumnTasks
                .Include(t => t.Owner)
                .Where(t => t.ColumnId == columnId)
                .ToListAsync();
        }

        public async Task<bool> Create(ColumnTask entity)
        {
            _logger.LogDebug("Creating a new task with name: {Name} in the database.", entity.Name);
            await _db.ColumnTasks.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(string id, ColumnTask entity)
        {
            _logger.LogDebug("Updating task with ID: {Id} in the database.", id);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return false;
            }

            existing.Name = entity.Name;
            existing.Description = entity.Description;
            existing.Text = entity.Text;

            _db.ColumnTasks.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateStatus(string id, ColumnTaskStatus status)
        {
            _logger.LogDebug("Updating status of task with ID: {Id} to {Status} in the database.", id, status);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return false;
            }

            existing.Status = status;
            _db.ColumnTasks.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdatePriority(string id, TaskPriority priority)
        {
            _logger.LogDebug("Updating priority of task with ID: {Id} to {Priority} in the database.", id, priority);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return false;
            }

            existing.Priority = priority;
            _db.ColumnTasks.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> MoveToColumn(string id, string columnId)
        {
            _logger.LogDebug("Moving task with ID: {Id} to column ID: {ColumnId} in the database.", id, columnId);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return false;
            }

            var columnExists = await _db.Columns.AnyAsync(c => c.Id == columnId);
            if (!columnExists)
            {
                _logger.LogWarning("Column with ID: {ColumnId} not found in the database.", columnId);
                return false;
            }

            existing.ColumnId = columnId;
            _db.ColumnTasks.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string id)
        {
            _logger.LogDebug("Deleting task with ID: {Id} from the database.", id);
            var existing = await _db.ColumnTasks.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task with ID: {Id} not found in the database.", id);
                return false;
            }

            _db.ColumnTasks.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
