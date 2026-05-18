using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;

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

        public async Task<RepositoryResult<List<TaskImages>>> GetAll()
        {
            _logger.LogDebug("Fetching all task images from the database.");
            try
            {
                var images = await _db.TaskImages.ToListAsync();
                return new RepositoryResult<List<TaskImages>>
                {
                    Success = true,
                    Data = images
                };

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task images from the database.");
                return new RepositoryResult<List<TaskImages>>
                {
                    Success = false,
                    Message = "An error occurred while fetching task images."
                };
            }
        }

        public async Task<RepositoryResult<TaskImages?>> GetById(string id)
        {
            _logger.LogDebug("Fetching task image with ID: {Id} from the database.", id);
            try
            {
                var image = await _db.TaskImages.FindAsync(id);
                return new RepositoryResult<TaskImages?>
                {
                    Success = true,
                    Data = image
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task image with ID: {Id} from the database.", id);
                return new RepositoryResult<TaskImages?>
                {
                    Success = false,
                    Message = "An error occurred while fetching task image."
                };
            }
        }

        public async Task<RepositoryResult<List<TaskImages>>> GetByTaskId(string taskId)
        {
            _logger.LogDebug("Fetching images for task ID: {TaskId} from the database.", taskId);
            try
            {
                var images = await _db.TaskImages.Where(i => i.TaskId == taskId).ToListAsync();
                return new RepositoryResult<List<TaskImages>>
                {
                    Success = true,
                    Data = images
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching task images for task ID: {TaskId} from the database.", taskId);
                return new RepositoryResult<List<TaskImages>>
                {
                    Success = false,
                    Message = "An error occurred while fetching task images."
                };
            }
        }

        public async Task<RepositoryResult<bool>> Create(TaskImages entity)
        {
            _logger.LogDebug("Creating a new image for task ID: {TaskId} in the database.", entity.TaskId);
            await _db.TaskImages.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create task image." };
        }

        public async Task<RepositoryResult<bool>> Update(string id, TaskImages entity)
        {
            _logger.LogDebug("Updating task image with ID: {Id} in the database.", id);
            var existing = await _db.TaskImages.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task image with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task image not found." };
            }

            existing.Name = entity.Name;
            existing.ImagePath = entity.ImagePath;

            _db.TaskImages.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update task image." };
        }

        public async Task<RepositoryResult<bool>> Delete(string id)
        {
            _logger.LogDebug("Deleting task image with ID: {Id} from the database.", id);
            var existing = await _db.TaskImages.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Task image with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Task image not found." };
            }

            _db.TaskImages.Remove(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to delete task image." };
        }
    }
}
