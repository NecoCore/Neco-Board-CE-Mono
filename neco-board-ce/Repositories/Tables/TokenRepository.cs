using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    /// <summary>
    /// Repository for managing refresh tokens in the database.
    /// </summary>
    public class TokenRepository
    {
        private readonly AppDbContext _db;
        private readonly ILogger<TokenRepository> _logger;

        public TokenRepository(AppDbContext db, ILogger<TokenRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RepositoryResult<RefreshTokens?>> GetByTokenHash(string hashedToken)
        {
            try
            {
                var token = await _db.RefreshTokens
                    .Include(t => t.Account)
                    .FirstOrDefaultAsync(t => t.Token == hashedToken);
                return new RepositoryResult<RefreshTokens?> { Success = true, Data = token };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching refresh token by hash.");
                return new RepositoryResult<RefreshTokens?> { Success = false, Message = "Database error." };
            }
        }

        public async Task<RepositoryResult<bool>> Create(RefreshTokens token)
        {
            try
            {
                await _db.RefreshTokens.AddAsync(token);
                var saved = await _db.SaveChangesAsync() > 0;
                return new RepositoryResult<bool> { Success = saved };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating refresh token.");
                return new RepositoryResult<bool> { Success = false, Message = "Failed to save token." };
            }
        }

        public async Task<RepositoryResult<bool>> Delete(RefreshTokens token)
        {
            try
            {
                _db.RefreshTokens.Remove(token);
                var saved = await _db.SaveChangesAsync() > 0;
                return new RepositoryResult<bool> { Success = saved };
            }
            catch (DbUpdateConcurrencyException)
            {
                return new RepositoryResult<bool> { Success = false, Message = "Token already deleted or modified." };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting refresh token.");
                return new RepositoryResult<bool> { Success = false, Message = "Database error." };
            }
        }

        public async Task DeleteExpiredAndUsed(Guid accountId)
        {
            try
            {
                var expired = _db.RefreshTokens.Where(t => t.AccountId == accountId && t.ExpiresAt < DateTime.UtcNow);
                _db.RefreshTokens.RemoveRange(expired);
                await _db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to cleanup expired tokens for account {AccountId}", accountId);
            }
        }
    }
}
