using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using neco_board_ce.Interfaces;
using neco_board_ce.Models.DTO.Request.Projects;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Utils
{
    public class ErrorHandlingTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;

        public ErrorHandlingTests(TestWebFactory factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task UnhandledException_ShouldReturnProblemDetails_RFC7807()
        {
            // Arrange
            var notifierMock = new Mock<IRealtimeNotifier>();
            // Make it throw so we get a 500 error handled by GlobalExceptionHandler
            notifierMock.Setup(n => n.ProjectCreated())
                .ThrowsAsync(new Exception("Simulated unhandled exception"));

            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IRealtimeNotifier>(notifierMock.Object);
                });
            });
            var client = factory.CreateClient();

            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
            
            // Re-ensure database is ready for this specific factory instance
            await db.Database.EnsureCreatedAsync();

            var admin = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Error Admin",
                Login = "error_admin_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.ADMIN
            };
            db.Accounts.Add(admin);
            await db.SaveChangesAsync();

            var token = jwtService.GenerateAccessToken(admin);
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new ProjectRequest { Name = "Failing Project" };

            // Act
            var response = await client.PostAsJsonAsync("/api/project", request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
            
            // 1. Check Content-Type (RFC 7807 specifies application/problem+json)
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");

            // 2. Verify the JSON structure follows ProblemDetails
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            problem.Should().NotBeNull();
            problem!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
            problem.Title.Should().Be("An unexpected error occurred");
            
            // 3. Verify that we are indeed getting a ProblemDetails object (contains standard fields)
            problem.Type.Should().NotBeNullOrEmpty();
        }
    }
}
