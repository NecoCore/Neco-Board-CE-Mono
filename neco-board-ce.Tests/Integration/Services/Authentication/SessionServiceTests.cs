using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Services.Authentication
{
    public class SessionServiceTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;

        public SessionServiceTests(TestWebFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RefreshSessionAsync_ShouldRotateTokens_AndInvalidateOldOne()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var user = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Session User",
                Login = "session_user_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };
            db.Accounts.Add(user);
            await db.SaveChangesAsync();

            // 1. Create initial session
            var (initialAccess, initialRefresh) = await sessionService.CreateSessionAsync(user);

            // 2. Perform first refresh (Rotation)
            var firstRefreshResult = await sessionService.RefreshSessionAsync(initialRefresh);
            
            // Assert: First refresh succeeded
            firstRefreshResult.Success.Should().BeTrue("First refresh with valid token should succeed");
            firstRefreshResult.RefreshToken.Should().NotBe(initialRefresh, "New refresh token should be different");

            // 3. Attempt to use the OLD refresh token again (Replay attack)
            var secondRefreshResult = await sessionService.RefreshSessionAsync(initialRefresh);

            // Assert: Second refresh with same token failed
            secondRefreshResult.Success.Should().BeFalse("Old refresh token should be deleted after use");
            secondRefreshResult.Error.Should().Be("Invalid or expired refresh token");
        }

        [Fact]
        public async Task RefreshSessionAsync_ShouldFail_WhenTokenIsExpired()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var sessionService = scope.ServiceProvider.GetRequiredService<SessionService>();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var user = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Expired User",
                Login = "expired_user_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };
            db.Accounts.Add(user);
            await db.SaveChangesAsync();

            // Manually create an expired token in DB
            var rawToken = "some_random_token";
            // Simple SHA256 hash to match SessionService logic
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var hash = Convert.ToBase64String(sha256.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawToken)));

            var expiredToken = new RefreshTokens
            {
                Id = Guid.NewGuid(),
                Token = hash,
                AccountId = user.Id,
                ExpiresAt = DateTime.UtcNow.AddHours(-1) // Expired 1 hour ago
            };
            db.RefreshTokens.Add(expiredToken);
            await db.SaveChangesAsync();

            // Act
            var result = await sessionService.RefreshSessionAsync(rawToken);

            // Assert
            result.Success.Should().BeFalse("Expired token should not be accepted");
            result.Error.Should().Be("Invalid or expired refresh token");
            
            // Verify it was deleted from DB after discovery
            db.RefreshTokens.Any(t => t.Id == expiredToken.Id).Should().BeFalse("Expired token should be cleaned up on attempt");
        }
    }
}
