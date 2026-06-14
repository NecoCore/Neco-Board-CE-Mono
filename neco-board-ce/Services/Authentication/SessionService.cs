using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Results;
using neco_board_ce.Repositories.Tables;
using System.Security.Cryptography;
using System.Text;

namespace neco_board_ce.Services.Authentication
{
    /// <summary>
    /// Service responsible for managing user sessions (Refresh Tokens).
    /// </summary>
    public class SessionService
    {
        private readonly TokenRepository _tokenRepository;
        private readonly JwtService _jwtService;
        private readonly IConfiguration _config;
        private readonly ILogger<SessionService> _logger;

        public SessionService(
            TokenRepository tokenRepository,
            JwtService jwtService,
            IConfiguration config,
            ILogger<SessionService> logger)
        {
            _tokenRepository = tokenRepository;
            _jwtService = jwtService;
            _config = config;
            _logger = logger;
        }

        public async Task<(string AccessToken, string RefreshToken)> CreateSessionAsync(Account account)
        {
            var accessToken = _jwtService.GenerateAccessToken(account);
            var rawRefreshToken = GenerateRandomToken();
            
            var refreshTokenEntity = new RefreshTokens
            {
                Token = HashToken(rawRefreshToken),
                AccountId = account.Id,
                ExpiresAt = DateTime.UtcNow.AddDays(_config.GetValue<int>("Jwt:RefreshTtl", 7))
            };

            // Cleanup expired tokens before creating a new one
            await _tokenRepository.DeleteExpiredAndUsed(account.Id);
            
            var result = await _tokenRepository.Create(refreshTokenEntity);
            if (!result.Success)
            {
                throw new Exception(result.Message ?? "Failed to save session.");
            }

            return (accessToken, rawRefreshToken);
        }

        public async Task<AuthResult> RefreshSessionAsync(string rawRefreshToken)
        {
            var hashedToken = HashToken(rawRefreshToken);
            var tokenResult = await _tokenRepository.GetByTokenHash(hashedToken);

            if (!tokenResult.Success || tokenResult.Data is null)
            {
                return new AuthResult(false, Error: "Invalid or expired refresh token");
            }

            var token = tokenResult.Data;

            if (token.ExpiresAt < DateTime.UtcNow)
            {
                await _tokenRepository.Delete(token);
                return new AuthResult(false, Error: "Invalid or expired refresh token");
            }

            // Rotate tokens: delete old one, create new one
            var deleteResult = await _tokenRepository.Delete(token);
            if (!deleteResult.Success)
            {
                return new AuthResult(false, Error: "Token was already used or is invalid.");
            }

            var (newAccess, newRefresh) = await CreateSessionAsync(token.Account);
            return new AuthResult(true, AccessToken: newAccess, RefreshToken: newRefresh);
        }

        public async Task RevokeSessionAsync(string rawRefreshToken)
        {
            var hashedToken = HashToken(rawRefreshToken);
            var tokenResult = await _tokenRepository.GetByTokenHash(hashedToken);

            if (tokenResult.Success && tokenResult.Data is not null)
            {
                await _tokenRepository.Delete(tokenResult.Data);
            }
        }

        private string GenerateRandomToken()
        {
            return Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        }

        private string HashToken(string token)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
            return Convert.ToBase64String(bytes);
        }
    }
}
