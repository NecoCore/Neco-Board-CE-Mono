using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;

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

        public async Task<RepositoryResult<List<UserProjectRole>>> GetByProjectId(string projectId)
        {
            _logger.LogDebug("Fetching members for project ID: {ProjectId} from the database.", projectId);
            try
            {
                var userProjectRoles = await _db.UserProjectRoles
                    .Include(r => r.User)
                    .Where(r => r.ProjectId == projectId)
                    .ToListAsync();
                return new RepositoryResult<List<UserProjectRole>> { Success = true, Data = userProjectRoles };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching members for project ID: {ProjectId}", projectId);
                return new RepositoryResult<List<UserProjectRole>>
                {
                    Success = false,
                    Message = "An error occurred while fetching members for project."
                };
            }
        }

        public async Task<RepositoryResult<List<UserProjectRole>>> GetByUserId(string userId)
        {
            _logger.LogDebug("Fetching project memberships for user ID: {UserId} from the database.", userId);
            try
            {
                var userProjectRoles = await _db.UserProjectRoles
                    .Include(r => r.Project)
                    .Where(r => r.UserId == userId)
                    .ToListAsync();
                return new RepositoryResult<List<UserProjectRole>> { Success = true, Data = userProjectRoles };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching project memberships for user ID: {UserId}", userId);
                return new RepositoryResult<List<UserProjectRole>>
                {
                    Success = false,
                    Message = "An error occurred while fetching project memberships for user."
                };
            }
        }

        public async Task<RepositoryResult<UserProjectRole?>> GetByUserAndProject(string userId, string projectId)
        {
            _logger.LogDebug("Fetching role for user ID: {UserId} in project ID: {ProjectId}.", userId, projectId);
            try
            {
                var userProjectRole = await _db.UserProjectRoles
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ProjectId == projectId);
                return new RepositoryResult<UserProjectRole?> { Success = true, Data = userProjectRole };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching role for user ID: {UserId} in project ID: {ProjectId}", userId, projectId);
                return new RepositoryResult<UserProjectRole?>
                {
                    Success = false,
                    Message = "An error occurred while fetching role for user."
                };
            }
        }

        public async Task<RepositoryResult<bool>> AddToProject(string userId, string projectId, ProjectRole role)
        {
            _logger.LogDebug("Adding user ID: {UserId} to project ID: {ProjectId} with role: {Role}.", userId, projectId, role);

            var alreadyMember = await _db.UserProjectRoles.AnyAsync(r => r.UserId == userId && r.ProjectId == projectId);
            if (alreadyMember)
            {
                _logger.LogWarning("User ID: {UserId} is already a member of project ID: {ProjectId}.", userId, projectId);
                return new RepositoryResult<bool> { Success = false, Message = "User is already a member of this project." };
            }

            var userExists = await _db.Accounts.AnyAsync(a => a.Id == userId);
            if (!userExists)
            {
                _logger.LogWarning("Account with ID: {UserId} not found in the database.", userId);
                return new RepositoryResult<bool> { Success = false, Message = "User not found." };
            }

            var projectExists = await _db.Projects.AnyAsync(p => p.Id == projectId);
            if (!projectExists)
            {
                _logger.LogWarning("Project with ID: {ProjectId} not found in the database.", projectId);
                return new RepositoryResult<bool> { Success = false, Message = "Project not found." };
            }

            await _db.UserProjectRoles.AddAsync(new UserProjectRole
            {
                UserId = userId,
                ProjectId = projectId,
                Role = role
            });
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to add user to project." };
        }

        public async Task<RepositoryResult<bool>> UpdateRole(string userId, string projectId, ProjectRole newRole)
        {
            _logger.LogDebug("Updating role of user ID: {UserId} in project ID: {ProjectId} to {Role}.", userId, projectId, newRole);

            var existing = await _db.UserProjectRoles.FirstOrDefaultAsync(r => r.UserId == userId && r.ProjectId == projectId);
            if (existing is null)
            {
                _logger.LogWarning("Membership of user ID: {UserId} in project ID: {ProjectId} not found.", userId, projectId);
                return new RepositoryResult<bool> { Success = false, Message = "Membership not found." };
            }

            existing.Role = newRole;
            _db.UserProjectRoles.Update(existing);
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to update role." };
        }

        public async Task<RepositoryResult<bool>> RemoveFromProject(string userId, string projectId)
        {
            _logger.LogDebug("Removing user ID: {UserId} from project ID: {ProjectId} in the database.", userId, projectId);

            var existing = await _db.UserProjectRoles.FirstOrDefaultAsync(r => r.UserId == userId && r.ProjectId == projectId);
            if (existing is null)
            {
                _logger.LogWarning("Membership of user ID: {UserId} in project ID: {ProjectId} not found.", userId, projectId);
                return new RepositoryResult<bool> { Success = false, Message = "Membership not found." };
            }

            _db.UserProjectRoles.Remove(existing);
            var result = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = result, Message = result ? string.Empty : "Failed to remove user from project." };
        }
    }
}
