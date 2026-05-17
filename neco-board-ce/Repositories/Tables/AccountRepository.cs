using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using System.Data;

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

        public async Task<List<Account>> GetAll()
        {
            _logger.LogDebug("Fetching all accounts from the database.");
            return await _db.Accounts.ToListAsync();
        }

        public async Task<Account?> GetById(string id)
        {
            _logger.LogDebug("Fetching account with ID {Id} from the database.", id);
            return await _db.Accounts.FindAsync(id);
        }

        public async Task<Account?> GetByLogin(string login)
        {
            _logger.LogDebug("Fetching account with login: {Login} from the database.", login);
            return await _db.Accounts.FirstOrDefaultAsync(a => a.Login == login);
        }

        public async Task<bool> Create(Account entity)
        {
            _logger.LogDebug("Creating a new account in the database.");
            await _db.Accounts.AddAsync(entity);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Update(string id, Account entity)
        {
            _logger.LogDebug("Updating account with ID {Id} in the database.", id);
            var existing = await _db.Accounts.FindAsync(id);
            if(existing is null)
            {
                _logger.LogInformation("Account with ID {Id} not found for update.", id);
                return false;
            }

            existing.Name = entity.Name;
            existing.Avatar = entity.Avatar;
            existing.Login = entity.Login;
            existing.Password = entity.Password;
            existing.Role = entity.Role;
    
            _db.Accounts.Update(existing);

            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdatePassword(string id, string newPassword)
        {
            _logger.LogDebug("Updating password for account with ID: {Id} in the database.", id);
            var existing = await _db.Accounts.FindAsync(id);
            if (existing is null)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return false;
            }

            existing.Password = newPassword;
            _db.Accounts.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateRole(string id, WorkspaceRoles role)
        {
            _logger.LogDebug("Updating role in {Role} user with ID: {ID}", role, id);
            var existing = await GetById(id);
            if(existing is null)
            {
                _logger.LogWarning("Account with ID: {Id} not found in the database.", id);
                return false;
            }
            existing.Role = role;
            _db.Accounts.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateAvatar(string id, string filePath)
        {
            _logger.LogDebug("Updating avatar user with ID: {ID}", id);
            var existing = await GetById(id);
            if(existing is null || filePath == null)
            {
                _logger.LogWarning("Not found account with id {id} or empty filePath {filePath}", id, filePath);
                return false;
            }
            existing.Avatar = filePath;
            _db.Accounts.Update(existing);
            return await _db.SaveChangesAsync() > 0;
        }

        public async Task<bool> Delete(string id)
        {
            _logger.LogDebug("Deleting account with ID {Id} from the database.", id);
            var account = await _db.Accounts.FirstOrDefaultAsync(a => a.Id == id);
            if (account == null)
            {
                _logger.LogInformation("Account with ID {Id} not found for deletion.", id);
                return false;
            }

            _db.Accounts.Remove(account);
            return await _db.SaveChangesAsync() > 0;
        }
    }
}