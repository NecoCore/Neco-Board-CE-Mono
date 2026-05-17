using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;

namespace neco_board_ce.Repositories.Tables
{
    public class TaskImagesRepository : ICRUDRepository<TaskImages>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TaskImagesRepository> _logger;

        public TaskImagesRepository(AppDbContext db, ILogger<TaskImagesRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<TaskImages>> GetAll()
        {
            _logger.LogDebug("Fetching all task images from the database.");
            return await _db.TaskImages.ToListAsync();
        }

        public async Task<TaskImages?> GetById(string id)
        {
            _logger.LogDebug("Fetching task image with ID: {Id} from the database.", id);
            return await _db.TaskImages.FindAsync(id);
        }

        public async Task<List<TaskImages>> GetByTaskId(string taskId)
        {
            _logger.LogDebug("Fetching images for task ID: {TaskId} from the database.", taskId);
            return await _db.TaskImages.Where(i => i.TaskId == taskId).ToListAsync();
        }

        public async Task<bool> Create(TaskImages entity)
        {
            _logger.LogDebug("Creating a new image for task ID: {TaskId} in the database.", entity.TaskId);
            await _db.TaskImages.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(string id, TaskImages entity)
        {
            _logger.LogDebug("Updating task image with ID: {Id} in the database.", id);
            var existing = await _db.TaskImages.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task image with ID: {Id} not found in the database.", id);
                return false;
            }

            existing.Name = entity.Name;
            existing.ImagePath = entity.ImagePath;

            _db.TaskImages.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string id)
        {
            _logger.LogDebug("Deleting task image with ID: {Id} from the database.", id);
            var existing = await _db.TaskImages.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task image with ID: {Id} not found in the database.", id);
                return false;
            }

            _db.TaskImages.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
