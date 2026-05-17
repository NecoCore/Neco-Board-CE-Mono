using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;

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

        public async Task<List<Column>> GetAll()
        {
            _logger.LogDebug("Fetching all columns from the database.");
            return await _db.Columns.ToListAsync();
        }

        public async Task<Column?> GetById(string id)
        {
            _logger.LogDebug("Fetching column with ID: {Id} from the database.", id);
            return await _db.Columns.FindAsync(id);
        }

        public async Task<List<Column>> GetByProjectId(string projectId)
        {
            _logger.LogDebug("Fetching columns for Project ID: {ProjectId} from the database.", projectId);
            return await _db.Columns.Where(c => c.ProjectId == projectId).ToListAsync();
        }

        public async Task<bool> Create(Column entity)
        {
            _logger.LogDebug("Creating a new column with Name: {Name} in the database.", entity.Name);
            await _db.Columns.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(string id, Column entity)
        {
            _logger.LogDebug("Updating column with ID: {Id} in the database.", id);
            var existing = await _db.Columns.FindAsync(id);
            if(existing is null)
            {
                _logger.LogWarning("Column with ID: {Id} not found in the database.", id); 
                return false;
            }

            existing.Name = entity.Name;

            _db.Columns.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateOrder(string projectId, string id, int newOrder)
        {
            _logger.LogDebug("Updating order of column with ID: {Id} to new order: {NewOrder} in the database.", id, newOrder);

            var existing = await GetByProjectId(projectId);
            if (existing is null || existing.Count == 0)
            {
                _logger.LogWarning("No columns found for Project ID: {ProjectId} in the database.", projectId);
                return false;
            }

            var columnToUpdate = existing.FirstOrDefault(c => c.Id == id);
            if (columnToUpdate is null)
            {
                _logger.LogWarning("Column with ID: {Id} not found in the database.", id);
                return false;
            }

            if (newOrder < 0 || newOrder >= existing.Count)
            {
                _logger.LogWarning("New order {NewOrder} is out of bounds for project {ProjectId}.", newOrder, projectId);
                return false;
            }

            if (columnToUpdate.Queue == newOrder)
            {
                _logger.LogInformation("Column with ID: {Id} already has the order: {NewOrder}. No update needed.", id, newOrder);
                return true;
            }
            else if (columnToUpdate.Queue > newOrder)
            {
                var columnsToShift = existing.Where(c => c.Queue >= newOrder && c.Queue < columnToUpdate.Queue).ToList();
                foreach (var column in columnsToShift)
                    column.Queue += 1;
                _db.Columns.UpdateRange(columnsToShift);
            }
            else
            {
                var columnsToShift = existing.Where(c => c.Queue > columnToUpdate.Queue && c.Queue <= newOrder).ToList();
                foreach (var column in columnsToShift)
                    column.Queue -= 1;
                _db.Columns.UpdateRange(columnsToShift);
            }

            columnToUpdate.Queue = newOrder;
            _db.Columns.Update(columnToUpdate);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string id)
        {
            _logger.LogDebug("Deleting column with ID: {Id} from the database.", id);
            var existing = await _db.Columns.FindAsync(id);
            if(existing is null)
            {
                _logger.LogWarning("Column with ID: {Id} not found in the database.", id); 
                return false;
            }

            _db.Columns.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
