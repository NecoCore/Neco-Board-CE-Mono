using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;

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

        public async Task<List<Project>> GetAll()
        {
            _logger.LogDebug("Fetching all projects from the database.");
            return await _db.Projects.ToListAsync();
        }

        public async Task<List<Project>?> GetAllByUserId(string id)
        {
            _logger.LogDebug("Fetching all projects for user with ID {UserId} from the database.", id);
            return await _db.UserProjectRoles
                .Where(upr => upr.UserId == id)
                .Select(upr => upr.Project)
                .ToListAsync();
        }

        public async Task<List<Project>> GetAllByOwnerId(string id)
        {
            _logger.LogDebug("Fetching all projects by owner with ID {OwnerId} from the database.", id);
            return await _db.Projects
                .Where(p => p.OwnerId == id)
                .ToListAsync();
        }

        public async Task<Project?> GetById(string id)
        {
            _logger.LogDebug("Fetching project with ID {ProjectId} from the database.", id);
            return await _db.Projects.FindAsync(id);
        }

        public async Task<bool> Create(Project entity)
        {
            _logger.LogDebug("Creating a new project in the database.");
            await _db.Projects.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(string id, Project entity)
        {
            _logger.LogDebug("Updating project with ID {ProjectId} in the database.", id);
            var existing = await _db.Projects.FindAsync(id);
            if (existing is null)
            {
                _logger.LogInformation("Project with ID {ProjectId} not found for update.", id);
                return false;
            }

            existing.Name = entity.Name;
            existing.Description = entity.Description;
            existing.OwnerId = entity.OwnerId == "" ? existing.OwnerId : entity.OwnerId;

            _db.Projects.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string id)
        {
            _logger.LogDebug("Deleting project with ID {ProjectId} from the database.", id);
            var project = await _db.Projects.FirstOrDefaultAsync(a => a.Id == id);
            if (project == null)
            {
                _logger.LogInformation("Project with ID {ProjectId} not found for deletion.", id);
                return false;
            }

            _db.Projects.Remove(project);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
