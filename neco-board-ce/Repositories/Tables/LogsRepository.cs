using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;

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

        public async Task<RepositoryResult<List<Logs>>> GetPage(int count, int page)
        {
            try
            {
                var logs = await _db.Logs
                    .Include(l => l.User)
                    .Include(l => l.Project)
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * count)
                    .Take(count)
                    .ToListAsync();

                return new RepositoryResult<List<Logs>> { Success = true, Data = logs };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated logs.");
                return new RepositoryResult<List<Logs>> { Success = false, Message = "Database error." };
            }
        }

        public async Task<RepositoryResult<List<Logs>>> GetByProjectId(Guid projectId, int count, int page)
        {
            try
            {
                var logs = await _db.Logs
                    .Include(l => l.User)
                    .Include(l => l.Project)
                    .Where(l => l.ProjectId == projectId)
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * count)
                    .Take(count)
                    .ToListAsync();

                return new RepositoryResult<List<Logs>> { Success = true, Data = logs };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated logs for project {ProjectId}.", projectId);
                return new RepositoryResult<List<Logs>> { Success = false, Message = "Database error." };
            }
        }

        public async Task<RepositoryResult<List<Logs>>> GetByUserId(Guid userId, int count, int page)
        {
            try
            {
                var logs = await _db.Logs
                    .Include(l => l.User)
                    .Include(l => l.Project)
                    .Where(l => l.UserId == userId)
                    .OrderByDescending(l => l.CreatedAt)
                    .Skip((page - 1) * count)
                    .Take(count)
                    .ToListAsync();

                return new RepositoryResult<List<Logs>> { Success = true, Data = logs };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching paginated logs for user {UserId}.", userId);
                return new RepositoryResult<List<Logs>> { Success = false, Message = "Database error." };
            }
        }

        public async Task<bool> Create(string name, Guid userId, LogType logType, LogFor logFor, string? description = null, Guid? newUserId = null, Guid? projectId = null)
        {
            try
            {
                await _db.Logs.AddAsync(new Logs
                {
                    Name = name,
                    UserId = userId,
                    ProjectId = projectId,
                    NewUserId = newUserId,
                    LogType = logType,
                    LogFor = logFor,
                    Description = description
                });

                return await _db.SaveChangesAsync() > 0;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log entry.");
                return false;
            }
        }
    }
}
