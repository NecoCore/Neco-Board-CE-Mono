using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.DTO.Request.Auth;
using neco_board_ce.Models.DTO.Response.Auth;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Data;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Controllers
{
    public class AuthControllerTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;
        private readonly HttpClient _client;

        public AuthControllerTests(TestWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Login_ShouldSetHttpOnlyRefreshTokenCookie()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            var password = "SecurePassword123!";
            var user = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Login = "testuser_" + Guid.NewGuid(),
                Password = BCrypt.Net.BCrypt.HashPassword(password),
                Role = WorkspaceRoles.USER
            };
            db.Accounts.Add(user);
            await db.SaveChangesAsync();

            var loginRequest = new LoginRequest
            {
                Login = user.Login,
                Password = password
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginRequest);

            // Assert
            response.EnsureSuccessStatusCode();
            
            // Check for the refreshToken cookie in the Set-Cookie header
            response.Headers.TryGetValues("Set-Cookie", out var cookies).Should().BeTrue();
            var cookieList = cookies!.ToList();
            var refreshCookie = cookieList.FirstOrDefault(c => c.StartsWith("refreshToken="));
            
            refreshCookie.Should().NotBeNull("refreshToken cookie should be set");
            refreshCookie.Should().ContainEquivalentOf("httponly", "The refresh token cookie must be HttpOnly for security");
        }
    }
}
