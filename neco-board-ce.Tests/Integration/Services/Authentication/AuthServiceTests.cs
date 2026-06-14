using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.DTO.Request.Auth;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Services.Authentication
{
    public class AuthServiceTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;

        public AuthServiceTests(TestWebFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenPasswordsDoNotMatch()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

            var request = new RegisterRequest
            {
                Name = "Test User",
                Login = "test_login_" + Guid.NewGuid(),
                Password = "Password123!",
                ConfirmPassword = "DifferentPassword123!"
            };

            // Act
            var result = await authService.RegisterAsync(request);

            // Assert
            result.Success.Should().BeFalse("Registration should fail when passwords do not match");
            result.Error.Should().Be("Passwords do not match");
        }

        [Fact]
        public async Task RegisterAsync_ShouldReturnFailure_WhenLoginIsAlreadyTaken()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var authService = scope.ServiceProvider.GetRequiredService<AuthService>();

            var login = "duplicate_login_" + Guid.NewGuid();
            var firstRequest = new RegisterRequest
            {
                Name = "First User",
                Login = login,
                Password = "Password123!",
                ConfirmPassword = "Password123!"
            };

            // First registration
            await authService.RegisterAsync(firstRequest);

            var secondRequest = new RegisterRequest
            {
                Name = "Second User",
                Login = login,
                Password = "OtherPassword123!",
                ConfirmPassword = "OtherPassword123!"
            };

            // Act
            var result = await authService.RegisterAsync(secondRequest);

            // Assert
            result.Success.Should().BeFalse("Registration should fail for duplicate login");
            result.Error.Should().Be("Login is already taken");
        }
    }
}
