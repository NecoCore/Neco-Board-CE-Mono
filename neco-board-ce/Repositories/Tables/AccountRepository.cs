using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Models.Results;

namespace neco_board_ce.Repositories.Tables
{
    /// <summary>
    /// Repository for performing CRUD operations and specialized queries on the accounts table (<see cref="Account"/>).
    /// </summary>
    /// <remarks>
    /// Implements <see cref="ICRUDRepository{Account}"/> to provide a standard interface for account management.
    /// Uses <see cref="AppDbContext"/> for database interaction and <see cref="ILogger{AccountRepository}"/> for operation tracking and error reporting.
    /// All methods return a <see cref="RepositoryResult{T}"/> to encapsulate success status and error messages.
    /// </remarks>
    public class AccountRepository : ICRUDRepository<Account>
    {
        private readonly AppDbContext _db;
        private readonly ILogger<AccountRepository> _logger;

        public AccountRepository(AppDbContext db, ILogger<AccountRepository> logger)
        {
            _db = db;
            _logger = logger;
        }

        /// <summary>
        /// Retrieves all accounts from the database.
        /// </summary>
        /// <returns>A <see cref="RepositoryResult{T}"/> containing the list of all <see cref="Account"/> entities on success.</returns>
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

        /// <summary>
        /// Retrieves a single account by its unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier (GUID) of the account.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> containing the <see cref="Account"/> entity if found; otherwise, success with null data.</returns>
        public async Task<RepositoryResult<Account?>> GetById(Guid id)
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

        /// <summary>
        /// Retrieves a paginated list of accounts.
        /// </summary>
        /// <param name="count">The number of records to take per page.</param>
        /// <param name="page">The 1-based index of the page to retrieve.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> containing the list of <see cref="Account"/> entities for the requested page.</returns>
        public async Task<RepositoryResult<List<Account>>> GetPage(int count, int page)
        {
            try
            {
                var accounts = await _db.Accounts.Skip((page - 1) * count).Take(count).ToListAsync();
                return new RepositoryResult<List<Account>> { Success = true, Data = accounts };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while fetching paginated accounts from the database.");
                return new RepositoryResult<List<Account>> { Success = false, Message = "An error occurred while fetching paginated accounts." };
            }
        }

        /// <summary>
        /// Searches for accounts by name query and optional workspace role filter.
        /// </summary>
        /// <param name="query">The search string to match against account names.</param>
        /// <param name="count">The number of records to take per page.</param>
        /// <param name="page">The 1-based index of the page to retrieve.</param>
        /// <param name="role">Optional filter for the <see cref="WorkspaceRoles"/>.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> containing the matching <see cref="Account"/> entities.</returns>
        public async Task<RepositoryResult<List<Account>>> SearchAccounts(string query, int count, int page, WorkspaceRoles? role = null)
        {
            try
            {
                var accounts = await _db.Accounts.Where(a => a.Name.Contains(query) && (role == null || a.Role == role)).Skip((page - 1) * count).Take(count).ToListAsync();
                return new RepositoryResult<List<Account>> { Success = true, Data = accounts };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while searching accounts with query {Query} in the database.", query);
                return new RepositoryResult<List<Account>> { Success = false, Message = "An error occurred while searching accounts." };
            }
        }

        /// <summary>
        /// Retrieves an account by its login name.
        /// </summary>
        /// <param name="login">The login name of the account.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> containing the <see cref="Account"/> entity if found.</returns>
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

        /// <summary>
        /// Creates a new account in the database.
        /// </summary>
        /// <param name="entity">The <see cref="Account"/> entity to create.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> indicating whether the operation succeeded.</returns>
        public async Task<RepositoryResult<bool>> Create(Account entity)
        {
            _logger.LogDebug("Creating a new account in the database.");
            await _db.Accounts.AddAsync(entity);
            var saved = await _db.SaveChangesAsync() > 0;
            return new RepositoryResult<bool> { Success = saved, Message = saved ? string.Empty : "Failed to create account." };
        }

        /// <summary>
        /// Updates an existing account's core details.
        /// </summary>
        /// <param name="id">The unique identifier of the account to update.</param>
        /// <param name="entity">The account entity containing updated values (Name, Avatar, Login, Password, Role).</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> indicating whether the update was successful.</returns>
        public async Task<RepositoryResult<bool>> Update(Guid id, Account entity)
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

        /// <summary>
        /// Updates only the password for a specific account.
        /// </summary>
        /// <param name="id">The unique identifier of the account.</param>
        /// <param name="newPassword">The new hashed password string.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> indicating whether the update was successful.</returns>
        public async Task<RepositoryResult<bool>> UpdatePassword(Guid id, string newPassword)
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

        /// <summary>
        /// Updates the workspace role for a specific account.
        /// </summary>
        /// <param name="id">The unique identifier of the account.</param>
        /// <param name="role">The new <see cref="WorkspaceRoles"/> to assign.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> indicating whether the update was successful.</returns>
        public async Task<RepositoryResult<bool>> UpdateRole(Guid id, WorkspaceRoles role)
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

        /// <summary>
        /// Updates the avatar file path for a specific account.
        /// </summary>
        /// <param name="id">The unique identifier of the account.</param>
        /// <param name="filePath">The new file path or URL for the user's avatar.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> indicating whether the update was successful.</returns>
        public async Task<RepositoryResult<bool>> UpdateAvatar(Guid id, string filePath)
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

        /// <summary>
        /// Deletes an account from the database.
        /// </summary>
        /// <param name="id">The unique identifier of the account to delete.</param>
        /// <returns>A <see cref="RepositoryResult{T}"/> indicating whether the deletion was successful.</returns>
        public async Task<RepositoryResult<bool>> Delete(Guid id)
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