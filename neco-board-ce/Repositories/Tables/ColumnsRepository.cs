using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    public class ColumnsRepository : ICRUDRepository<Column>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ColumnsRepository> _logger;

        public ColumnsRepository(AppDbContext db, ILogger<ColumnsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RepositoryResult<List<Column>>> GetAll()
        {
            _logger.LogDebug("Fetching all columns from the database.");
            try
            {
                var columns = await _db.Columns.ToListAsync();
                return new RepositoryResult<List<Column>> { Success = true, Data = columns };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching columns from the database.");
                return new RepositoryResult<List<Column>> { Success = false, Message = "An error occurred while fetching columns." };
            }
        }

        public async Task<RepositoryResult<Column?>> GetById(string id)
        {
            _logger.LogDebug("Fetching column with ID: {Id} from the database.", id);
            try
            {
                var column = await _db.Columns.FindAsync(id);
                return new RepositoryResult<Column?> { Success = true, Data = column };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching column with ID: {Id} from the database.", id);
                return new RepositoryResult<Column?> { Success = false, Message = "An error occurred while fetching column." };
            }
        }

        public async Task<RepositoryResult<List<Column>>> GetByProjectId(string projectId)
        {
            _logger.LogDebug("Fetching columns for Project ID: {ProjectId} from the database.", projectId);
            try
            {
                var columns = await _db.Columns.Where(c => c.ProjectId == projectId).ToListAsync();
                return new RepositoryResult<List<Column>> { Success = true, Data = columns };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching columns for Project ID: {ProjectId} from the database.", projectId);
                return new RepositoryResult<List<Column>> { Success = false, Message = "An error occurred while fetching columns." };
            }
        }

        public async Task<RepositoryResult<bool>> Create(Column entity)
        {
            _logger.LogDebug("Creating a new column with Name: {Name} in the database.", entity.Name);
            await _db.Columns.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create column." };
        }

        public async Task<RepositoryResult<bool>> Update(string id, Column entity)
        {
            _logger.LogDebug("Updating column with ID: {Id} in the database.", id);
            var existing = await _db.Columns.FindAsync(id);
            if(existing is null)
            {
                _logger.LogWarning("Column with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Column not found." };
            }

            existing.Name = entity.Name;

            _db.Columns.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update column." };
        }

        public async Task<RepositoryResult<bool>> UpdateOrder(string projectId, string id, int newOrder)
        {
            _logger.LogDebug("Updating order of column with ID: {Id} to new order: {NewOrder} in the database.", id, newOrder);

            var existing = await GetByProjectId(projectId);
            if (!existing.Success)
            {
                _logger.LogWarning("No columns found for Project ID: {ProjectId} in the database.", projectId);
                return new RepositoryResult<bool> { Success = false, Message = existing.Message };
            }
            if(existing.Data is null || existing.Data.Count == 0)
            {
                _logger.LogWarning("No columns found for Project ID: {ProjectId} in the database.", projectId);
                return new RepositoryResult<bool> { Success = false, Message = "No columns found for the specified project." };
            }

            var columnToUpdate = existing.Data.FirstOrDefault(c => c.Id == id);
            if (columnToUpdate is null)
            {
                _logger.LogWarning("Column with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Column not found." };
            }

            if (newOrder < 0 || newOrder >= existing.Data.Count)
            {
                _logger.LogWarning("New order {NewOrder} is out of bounds for project {ProjectId}.", newOrder, projectId);
                return new RepositoryResult<bool> { Success = false, Message = "New order is out of bounds." };
            }

            if (columnToUpdate.Queue == newOrder)
            {
                _logger.LogInformation("Column with ID: {Id} already has the order: {NewOrder}. No update needed.", id, newOrder);
                return new RepositoryResult<bool> { Success = true, Message = string.Empty };
            }
            else if (columnToUpdate.Queue > newOrder)
            {
                var columnsToShift = existing.Data.Where(c => c.Queue >= newOrder && c.Queue < columnToUpdate.Queue).ToList();
                foreach (var column in columnsToShift)
                    column.Queue += 1;
                _db.Columns.UpdateRange(columnsToShift);
            }
            else
            {
                var columnsToShift = existing.Data.Where(c => c.Queue > columnToUpdate.Queue && c.Queue <= newOrder).ToList();
                foreach (var column in columnsToShift)
                    column.Queue -= 1;
                _db.Columns.UpdateRange(columnsToShift);
            }

            columnToUpdate.Queue = newOrder;
            _db.Columns.Update(columnToUpdate);
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to update column order." };
        }

        public async Task<RepositoryResult<bool>> Delete(string id)
        {
            _logger.LogDebug("Deleting column with ID: {Id} from the database.", id);
            var existing = await _db.Columns.FindAsync(id);
            if(existing is null)
            {
                _logger.LogWarning("Column with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Column not found." };
            }

            _db.Columns.Remove(existing);
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to delete column." };
        }
    }
}
