using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    public class AccountRepository : ICRUDRepository<Account>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(AppDbContext db, ILogger<AccountRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        public async Task<RepositoryResult<List<Account>>> GetAll()
        {
            _logger.LogDebug("Fetching all accounts from the database.");
            try
            {
                var accounts = await _db.Accounts.ToListAsync();
                return new RepositoryResult<List<Account>> { Success = true, Data = accounts };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching accounts from the database.");
                return new RepositoryResult<List<Account>> { Success = false, Message = "An error occurred while fetching accounts." };
            }
        }

        public async Task<RepositoryResult<Account?>> GetById(string id)
        {
            _logger.LogDebug("Fetching account with ID {Id} from the database.", id);
            try
            {
                var account = await _db.Accounts.FindAsync(id);
                return new RepositoryResult<Account?> { Success = true, Data = account };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching account with ID {Id} from the database.", id);
                return new RepositoryResult<Account?> { Success = false, Message = "An error occurred while fetching account." };
            }
        }

        public async Task<RepositoryResult<Account?>> GetByLogin(string login)
        {
            _logger.LogDebug("Fetching account with login: {Login} from the database.", login);
            try
            {
                var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Login == login);
                return new RepositoryResult<Account?> { Success = true, Data = account };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching account with login {Login} from the database.", login);
                return new RepositoryResult<Account?> { Success = false, Message = "An error occurred while fetching account." };
            }
        }

        public async Task<RepositoryResult<bool>> Create(Account entity)
        {
            _logger.LogDebug("Creating a new account in the database.");
            await _db.Accounts.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create account." };
        }

        public async Task<RepositoryResult<bool>> Update(string id, Account entity)
        {
            _logger.LogDebug("Updating account with ID {Id} in the database.", id);
            var existing = await _db.Accounts.FindAsync(id);
            if(existing is null)
            {
                _logger.LogInformation("Account with ID {Id} not found for update.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Account not found." };
            }

            existing.Name = entity.Name;
            existing.Avatar = entity.Avatar;
            existing.Login = entity.Login;
            existing.Password = entity.Password;
            existing.Role = entity.Role;
    
            _db.Accounts.Update(existing);

            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update account." };
        }

        public async Task<RepositoryResult<bool>> UpdatePassword(string id, string newPassword)
        {
            _logger.LogDebug("Updating password for account with ID: {Id} in the database.", id);
            var existing = await _db.Accounts.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Account not found." };
            }

            existing.Password = newPassword;
            _db.Accounts.Update(existing);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update password." };
        }

        public async Task<RepositoryResult<bool>> UpdateRole(string id, WorkspaceRoles role)
        {
            _logger.LogDebug("Updating role in {Role} user with ID: {ID}", role, id);
            var existing = await GetById(id);
            if(!existing.Success)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = existing.Message };
            }
            if(existing.Data is null)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Account not found." };
            }

            existing.Data.Role = role;
            _db.Accounts.Update(existing.Data);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update role." };
        }

        public async Task<RepositoryResult<bool>> UpdateAvatar(string id, string filePath)
        {
            _logger.LogDebug("Updating avatar user with ID: {ID}", id);
            var existing = await GetById(id);
            if (!existing.Success)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = existing.Message };
            }
            if (existing.Data is null)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Account not found." };
            }

            existing.Data.Avatar = filePath;
            _db.Accounts.Update(existing.Data);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to update avatar." };
        }

        public async Task<RepositoryResult<bool>> Delete(string id)
        {
            _logger.LogDebug("Deleting account with ID {Id} from the database.", id);
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
            if (account == null)
            {
                _logger.LogInformation("Account with ID {Id} not found for deletion.", id);
                return new RepositoryResult<bool> { Success = false, Message = "Account not found." };
            }

            _db.Accounts.Remove(account);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to delete account." };
        }
    }
}