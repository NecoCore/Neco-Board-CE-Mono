using neco_board_ce.Models.DTO.Request.Auth;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;
using neco_board_ce.Repositories.Tables;

namespace neco_board_ce.Services.Authentication
{
    /// <summary>
    /// Service responsible for high-level authentication workflows: Login and Registration.
    /// It delegates session management to <see cref="SessionService"/>.
    /// </summary>
    public class AuthService
    {
        private readonly AccountRepository _accountRepository;
        private readonly SessionService _sessionService;

        public AuthService(AccountRepository accountRepository, SessionService sessionService)
        {
            _accountRepository = accountRepository;
            _sessionService = sessionService;
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

            var (accessToken, refreshToken) = await _sessionService.CreateSessionAsync(account);

            return new AuthResult(true, AccessToken: accessToken, RefreshToken: refreshToken);
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

            var (accessToken, refreshToken) = await _sessionService.CreateSessionAsync(account);

            return new AuthResult(true, AccessToken: accessToken, RefreshToken: refreshToken);
        }

        public async Task<AuthResult> RefreshAsync(string refreshToken)
        {
            return await _sessionService.RefreshSessionAsync(refreshToken);
        }

        public async Task RevokeAsync(string refreshToken)
        {
            await _sessionService.RevokeSessionAsync(refreshToken);
        }
    }
}
