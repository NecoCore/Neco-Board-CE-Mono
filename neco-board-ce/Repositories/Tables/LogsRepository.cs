using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Repositories.Tables
{
    public class LogsRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<LogsRepository> _logger;

        public LogsRepository(AppDbContext db, ILogger<LogsRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<Logs>> GetAll()
        {
            _logger.LogDebug("Fetching all logs from the database.");
            return await _db.Logs
                .Include(l => l.User)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Logs>> GetByProjectId(string projectId)
        {
            _logger.LogDebug("Fetching logs for project ID: {ProjectId} from the database.", projectId);
            return await _db.Logs
                .Include(l => l.User)
                .Where(l => l.ProjectId == projectId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<Logs>> GetByUserId(string userId)
        {
            _logger.LogDebug("Fetching logs for user ID: {UserId} from the database.", userId);
            return await _db.Logs
                .Where(l => l.UserId == userId)
                .OrderByDescending(l => l.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> Create(string name, string userId, string projectId, LogType logType, string? description = null)
        {
            _logger.LogDebug("Creating log for user ID: {UserId} in project ID: {ProjectId}.", userId, projectId);

            await _db.Logs.AddAsync(new Logs
            {
                Name = name,
                UserId = userId,
                ProjectId = projectId,
                LogType = logType,
                Description = description
            });

            return await _db.SaveChangesAsync() > 0;
        }
    }
}
