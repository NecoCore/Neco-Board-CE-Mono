using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using System.Security.Cryptography;

namespace neco_board_ce.Services.Authentication
{
    public class JwtService
    {
        private readonly IConfiguration _config;
        private readonly AppDbContext _db;

        public JwtService(IConfiguration config, AppDbContext db)
        {
            _config = config;
            _db = db;
        }

        public string GenerateAccessToken(Account account)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, account.Id),
                new Claim(ClaimTypes.Name, account.Login),
                new Claim(ClaimTypes.Role, account.Role.ToString()),
                new Claim("name", account.Name),
                new Claim("avatar", account.Avatar ?? string.Empty)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Secret"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.UtcNow.AddMinutes(_config.GetValue<int>("Jwt:AccessTtl", 15));

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        public async Task<RefreshTokens> GenerateRefreshToken(string accountId)
        {
            var refreshToken = new RefreshTokens
            {
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                AccountId = accountId,
                ExpiresAt = DateTime.UtcNow.AddDays(
                    _config.GetValue<int>("Jwt:RefreshTtl", 7)
                )
            };

            _db.RefreshTokens.RemoveRange(
                _db.RefreshTokens.Where(t => t.AccountId == accountId && t.ExpiresAt < DateTime.UtcNow)
            );

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return refreshToken;
        }

        public string? GetAccountId(ClaimsPrincipal user) =>
            user.FindFirstValue(ClaimTypes.NameIdentifier);
    }
}
