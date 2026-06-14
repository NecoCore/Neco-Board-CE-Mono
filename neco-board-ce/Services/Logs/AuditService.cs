using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;

namespace neco_board_ce.Services.Logs
{
    public class AuditService
    {
        private readonly ILogger _logger;
        private readonly LogsRepository _repository;

        public AuditService(ILogger<AuditService> logger, LogsRepository repository)
        {
            _logger = logger;
            _repository = repository;
        }

        public async Task ProjectLog(Guid projectId, Guid userId, LogType type, string name, string? description = null)
        {
            try
            {
                await _repository.Create(name, userId, type, LogFor.PROJECT, description, projectId: projectId);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating project log");
                throw;
            }
        }
        public async Task FileLog(Guid projectId, Guid userId, LogType type, string name, string? description = null)
        {
            try
            {
                await _repository.Create(name, userId, type, LogFor.FILE, description, projectId: projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating file log");
                throw;
            }
        }
        public async Task ColumnLog(Guid projectId, Guid userId, LogType type, string name, string? description = null)
        {
            try
            {
                await _repository.Create(name, userId, type, LogFor.COLUMN, description, projectId: projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating column log");
                throw;
            }
        }

        public async Task TaskLog(Guid projectId, Guid userId, LogType type, string name, string? description = null)
        {
            try
            {
                await _repository.Create(name, userId, type, LogFor.TASK, description, projectId: projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating task log");
                throw;
            }
        }

        public async Task UserLog(Guid newUserId, Guid userId, LogType type, string name, string? description = null)
        {
            try
            {
                await _repository.Create(name, userId, type, LogFor.USER, description, newUserId: newUserId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user log");
                throw;
            }
        }

        public async Task Log(Guid userId, LogType type, string name, LogFor logFor, string? description = null, Guid? newUserId = null, Guid? projectId = null)
        {
            try
            {
                await _repository.Create(name, userId, type, logFor, description, newUserId: newUserId, projectId: projectId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating log");
                throw;
            }
        }
    }
}
