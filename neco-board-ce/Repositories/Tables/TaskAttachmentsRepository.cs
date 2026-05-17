using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;

namespace neco_board_ce.Repositories.Tables
{
    public class TaskAttachmentsRepository : ICRUDRepository<TaskAttachments>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TaskAttachmentsRepository> _logger;

        public TaskAttachmentsRepository(AppDbContext db, ILogger<TaskAttachmentsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<TaskAttachments>> GetAll()
        {
            _logger.LogDebug("Fetching all task attachments from the database.");
            return await _db.TaskAttachments.ToListAsync();
        }

        public async Task<TaskAttachments?> GetById(string id)
        {
            _logger.LogDebug("Fetching task attachment with ID: {Id} from the database.", id);
            return await _db.TaskAttachments.FindAsync(id);
        }

        public async Task<List<TaskAttachments>> GetByTaskId(string taskId)
        {
            _logger.LogDebug("Fetching attachments for task ID: {TaskId} from the database.", taskId);
            return await _db.TaskAttachments.Where(a => a.TaskId == taskId).ToListAsync();
        }

        public async Task<bool> Create(TaskAttachments entity)
        {
            _logger.LogDebug("Creating a new attachment for task ID: {TaskId} in the database.", entity.TaskId);
            await _db.TaskAttachments.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(string id, TaskAttachments entity)
        {
            _logger.LogDebug("Updating task attachment with ID: {Id} in the database.", id);
            var existing = await _db.TaskAttachments.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task attachment with ID: {Id} not found in the database.", id);
                return false;
            }

            existing.Name = entity.Name;
            existing.Type = entity.Type;
            existing.FilePath = entity.FilePath;

            _db.TaskAttachments.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string id)
        {
            _logger.LogDebug("Deleting task attachment with ID: {Id} from the database.", id);
            var existing = await _db.TaskAttachments.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task attachment with ID: {Id} not found in the database.", id);
                return false;
            }

            _db.TaskAttachments.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
