using Microsoft.EntityFrameworkCore;
using neco_board_ce.Data;
using neco_board_ce.Models.DTO.Request.Auth;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;
using neco_board_ce.Repositories.Tables;

namespace neco_board_ce.Services.Authentication
{
    public class AuthService
    {
        private readonly AccountRepository _accountRepository;
        private readonly AppDbContext _db;
        private readonly JwtService _jwtService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(AccountRepository accountRepository, JwtService jwtService, AppDbContext db, ILogger<AuthService> logger)
        {
            _accountRepository = accountRepository;
            _jwtService = jwtService;
            _db = db;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(RegisterRequest dto)
        {
            var existing = await _accountRepository.GetByLogin(dto.Login);
            if (!existing.Success) return new AuthResult(false, Error: existing.Message);
            if (existing.Data is not null) return new AuthResult(false, Error: "Login is already taken");

            if (dto.Password != dto.ConfirmPassword)
                return new AuthResult(false, Error: "Passwords do not match");

            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Login = dto.Login,
                Password = BCrypt.Net.BCrypt.HashPassword(dto.Password)
            };

            await _accountRepository.Create(account);

            var accessToken = _jwtService.GenerateAccessToken(account);
            var (_, rawToken) = await _jwtService.GenerateRefreshToken(account.Id);

            return new AuthResult(true, AccessToken: accessToken, RefreshToken: rawToken);
        }

        public async Task<AuthResult> LoginAsync(LoginRequest dto)
        {
            var result = await _accountRepository.GetByLogin(dto.Login);
            if(!result.Success) return new AuthResult(false, Error: result.Message);

            var account = result.Data;
            if (account is null || !BCrypt.Net.BCrypt.Verify(dto.Password, account.Password))
                return new AuthResult(false, Error: "Invalid login or password");

            account.LastLoginAt = DateTime.UtcNow;
            await _accountRepository.Update(account.Id, account);

            var accessToken = _jwtService.GenerateAccessToken(account);
            var (_, rawToken) = await _jwtService.GenerateRefreshToken(account.Id);

            return new AuthResult(true, AccessToken: accessToken, RefreshToken: rawToken);
        }

        public async Task<AuthResult> RefreshAsync(string refreshToken)
        {
            var hashedToken = _jwtService.HashToken(refreshToken);
            var token = await _db.RefreshTokens
                .Include(t => t.Account)
                .FirstOrDefaultAsync(t => t.Token == hashedToken);

            if (token is null)
            {
                return new AuthResult(false, Error: "Invalid or expired refresh token");
            }
            if (token.ExpiresAt < DateTime.UtcNow)
            {
                try
                {
                    _db.RefreshTokens.Remove(token);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException err)
                {
                    _logger.LogError("Failed to delete expired refresh token: {}", err.Message);
                }

                return new AuthResult(false, Error: "Invalid or expired refresh token");
            }

            try
            {
                _db.RefreshTokens.Remove(token);
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException err)
            {
                _logger.LogWarning("Failed to delete refresh token after checks: {}", err.Message);
                return new AuthResult(false, Error: "Token was already used by another concurrent request");
            }

            var accessToken = _jwtService.GenerateAccessToken(token.Account);
            var (_, newRawToken) = await _jwtService.GenerateRefreshToken(token.Account.Id);

            return new AuthResult(true, AccessToken: accessToken, RefreshToken: newRawToken);
        }

        public async Task RevokeAsync(string refreshToken)
        {
            var hashedToken = _jwtService.HashToken(refreshToken);
            var token = await _db.RefreshTokens
                .FirstOrDefaultAsync(t => t.Token == hashedToken);

            if (token is not null)
            {
                try
                {
                    _db.RefreshTokens.Remove(token);
                    await _db.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException err)
                {
                    _logger.LogWarning("Failed to delete refresh token after logout: {Error}", err.Message);
                }
            }
        }
    }
}
