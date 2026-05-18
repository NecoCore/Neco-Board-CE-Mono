using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    public class ProjectRepository : ICRUDRepository<Project>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<ProjectRepository> _logger;

        public ProjectRepository(AppDbContext db, ILogger<ProjectRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RepositoryResult<List<Project>>> GetAll()
        {
            _logger.LogDebug("Fetching all projects from the database.");
            try
            {
                var result = await _db.Projects.ToListAsync();
                return new RepositoryResult<List<Project>> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching projects from the database.");
                return new RepositoryResult<List<Project>> { Success = false, Message = "An error occurred while fetching projects." };
            }
        }

        public async Task<RepositoryResult<List<Project>>> GetAllByUserId(string id)
        {
            _logger.LogDebug("Fetching all projects for user with ID {UserId} from the database.", id);
            try
            {
                var result = await _db.UserProjectRoles
                    .Where(upr => upr.UserId == id)
                    .Select(upr => upr.Project)
                    .ToListAsync();
                return new RepositoryResult<List<Project>> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching projects for user with ID {UserId} from the database.", id);
                return new RepositoryResult<List<Project>> { Success = false, Message = "An error occurred while fetching projects." };
            }
        }

        public async Task<RepositoryResult<List<Project>>> GetAllByOwnerId(string id)
        {
            _logger.LogDebug("Fetching all projects by owner with ID {OwnerId} from the database.", id);
            try
            {
                var result = await _db.Projects
                    .Where(p => p.OwnerId == id)
                    .ToListAsync();
                return new RepositoryResult<List<Project>> { Success = true, Data = result };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching projects by owner with ID {OwnerId} from the database.", id);
                return new RepositoryResult<List<Project>> { Success = false, Message = "An error occurred while fetching projects." };
            }
        }

        public async Task<RepositoryResult<Project?>> GetById(string id)
        {
            _logger.LogDebug("Fetching project with ID {ProjectId} from the database.", id);
            try
            {
                var project = await _db.Projects.FindAsync(id);
                return new RepositoryResult<Project?> { Success = true, Data = project };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching project with ID {ProjectId} from the database.", id);
                return new RepositoryResult<Project?> { Success = false, Message = "An error occurred while fetching project." };
            }
        }

        public async Task<RepositoryResult<bool>> Create(Project entity)
        {
            _logger.LogDebug("Creating a new project in the database.");
            await _db.Projects.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create project." };
        }

        public async Task<RepositoryResult<bool>> Update(string id, Project entity)
        {
            _logger.LogDebug("Updating project with ID {ProjectId} in the database.", id);
            var existing = await _db.Projects.FindAsync(id);
            if (existing is null)
            {
                _logger.LogInformation("Project with ID {ProjectId} not found for update.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Project not found." };
            }

            existing.Name = entity.Name;
            existing.Description = entity.Description;
            existing.OwnerId = entity.OwnerId == "" ? existing.OwnerId : entity.OwnerId;

            _db.Projects.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update project." };
        }

        public async Task<RepositoryResult<bool>> Delete(string id)
        {
            _logger.LogDebug("Deleting project with ID {ProjectId} from the database.", id);
            var project = await _db.Projects.FirstOrDefaultAsync(a => a.Id == id);
            if (project == null)
            {
                _logger.LogInformation("Project with ID {ProjectId} not found for deletion.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Project not found." };
            }

            _db.Projects.Remove(project);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to delete project." };
        }
    }
}
