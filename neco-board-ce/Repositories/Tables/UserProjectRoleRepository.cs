using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;

namespace neco_board_ce.Repositories.Tables
{
    public class UserProjectRoleRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<UserProjectRoleRepository> _logger;

        public UserProjectRoleRepository(AppDbContext db, ILogger<UserProjectRoleRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<List<UserProjectRole>> GetByProjectId(string projectId)
        {
            _logger.LogDebug("Fetching members for project ID: {ProjectId} from the database.", projectId);
            return await _db.UserProjectRoles
                .Include(r => r.User)
                .Where(r => r.ProjectId == projectId)
                .ToListAsync();
        }

        public async Task<List<UserProjectRole>> GetByUserId(string userId)
        {
            _logger.LogDebug("Fetching project memberships for user ID: {UserId} from the database.", userId);
            return await _db.UserProjectRoles
                .Include(r => r.Project)
                .Where(r => r.UserId == userId)
                .ToListAsync();
        }

        public async Task<UserProjectRole?> GetByUserAndProject(string userId, string projectId)
        {
            _logger.LogDebug("Fetching role for user ID: {UserId} in project ID: {ProjectId}.", userId, projectId);
            return await _db.UserProjectRoles
                .FirstOrDefaultAsync(r => r.UserId == userId && r.ProjectId == projectId);
        }

        public async Task<bool> AddToProject(string userId, string projectId, ProjectRole role)
        {
            _logger.LogDebug("Adding user ID: {UserId} to project ID: {ProjectId} with role: {Role}.", userId, projectId, role);

            var alreadyMember = await _db.UserProjectRoles.AnyAsync(r => r.UserId == userId && r.ProjectId == projectId);
            if (alreadyMember)
            {
                _logger.LogWarning("User ID: {UserId} is already a member of project ID: {ProjectId}.", userId, projectId);
                return false;
            }

            var userExists = await _db.Accounts.AnyAsync(a => a.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Account with ID: {UserId} not found in the database.", userId);
                return false;
            }

            var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists)
            {
                _logger.LogWarning("Project with ID: {ProjectId} not found in the database.", projectId);
                return false;
            }

            await _db.UserProjectRoles.AddAsync(new UserProjectRole
            {
                UserId = userId,
                ProjectId = projectId,
                Role = role
            });
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateRole(string userId, string projectId, ProjectRole newRole)
        {
            _logger.LogDebug("Updating role of user ID: {UserId} in project ID: {ProjectId} to {Role}.", userId, projectId, newRole);

            var existing = await _db.UserProjectRoles.FirstOrDefaultAsync(r => r.UserId == userId && r.ProjectId == projectId);
            if (existing is null)
            {
                _logger.LogWarning("Membership of user ID: {UserId} in project ID: {ProjectId} not found.", userId, projectId);
                return false;
            }

            existing.Role = newRole;
            _db.UserProjectRoles.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> RemoveFromProject(string userId, string projectId)
        {
            _logger.LogDebug("Removing user ID: {UserId} from project ID: {ProjectId} in the database.", userId, projectId);

            var existing = await _db.UserProjectRoles.FirstOrDefaultAsync(r => r.UserId == userId && r.ProjectId == projectId);
            if (existing is null)
            {
                _logger.LogWarning("Membership of user ID: {UserId} in project ID: {ProjectId} not found.", userId, projectId);
                return false;
            }

            _db.UserProjectRoles.Remove(existing);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}
