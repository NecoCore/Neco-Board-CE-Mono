using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;

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

        public async Task<RepositoryResult<List<TaskAttachments>>> GetAll()
        {
            _logger.LogDebug("Fetching all task attachments from the database.");
            try
            {
                var result = await _db.TaskAttachments.ToListAsync();
                return new RepositoryResult<List<TaskAttachments>> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task attachments from the database.");
                return new RepositoryResult<List<TaskAttachments>> { Success = false, Message = "An error occurred while fetching task attachments." };
            }
        }

        public async Task<RepositoryResult<TaskAttachments?>> GetById(string id)
        {
            _logger.LogDebug("Fetching task attachment with ID: {Id} from the database.", id);
            try
            {
                var result = await _db.TaskAttachments.FindAsync(id);
                return new RepositoryResult<TaskAttachments?> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task attachment with ID: {Id} from the database.", id);
                return new RepositoryResult<TaskAttachments?> { Success = false, Message = "An error occurred while fetching task attachment." };
            }
        }

        public async Task<RepositoryResult<List<TaskAttachments>>> GetByTaskId(string taskId)
        {
            _logger.LogDebug("Fetching attachments for task ID: {TaskId} from the database.", taskId);
            try
            {
                var result = await _db.TaskAttachments.Where(a => a.TaskId == taskId).ToListAsync();
                return new RepositoryResult<List<TaskAttachments>> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task attachments for task ID: {TaskId} from the database.", taskId);
                return new RepositoryResult<List<TaskAttachments>> { Success = false, Message = "An error occurred while fetching task attachments." };
            }
        }

        public async Task<RepositoryResult<bool>> Create(TaskAttachments entity)
        {
            _logger.LogDebug("Creating a new attachment for task ID: {TaskId} in the database.", entity.TaskId);
            await _db.TaskAttachments.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create task attachment." };
        }

        public async Task<RepositoryResult<bool>> Update(string id, TaskAttachments entity)
        {
            _logger.LogDebug("Updating task attachment with ID: {Id} in the database.", id);
            var existing = await _db.TaskAttachments.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task attachment with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task attachment not found." };
            }

            existing.Name = entity.Name;
            existing.Type = entity.Type;
            existing.FilePath = entity.FilePath;

            _db.TaskAttachments.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update task attachment." };
        }

        public async Task<RepositoryResult<bool>> Delete(string id)
        {
            _logger.LogDebug("Deleting task attachment with ID: {Id} from the database.", id);
            var existing = await _db.TaskAttachments.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task attachment with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task attachment not found." };
            }

            _db.TaskAttachments.Remove(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to delete task attachment." };
        }
    }
}
