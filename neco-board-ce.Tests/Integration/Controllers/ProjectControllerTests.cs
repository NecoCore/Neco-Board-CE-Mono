using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.DTO.Request.Projects;
using neco_board_ce.Models.DTO.Response.Projects;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Controllers
{
    public class ProjectControllerTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;
        private readonly HttpClient _client;

        public ProjectControllerTests(TestWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task GetProjectById_ShouldReturnForbidden_WhenUserIsNotMember()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
            await db.Database.EnsureCreatedAsync();

            // 1. Create User A (The Attacker)
            var userA = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Attacker User",
                Login = "attacker_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };

            // 2. Create User B (The Victim) and their Project
            var userB = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Victim User",
                Login = "victim_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };

            var projectB = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Victim's Private Project",
                OwnerId = userB.Id
            };

            db.Accounts.AddRange(userA, userB);
            db.Projects.Add(projectB);
            await db.SaveChangesAsync();

            // 3. Generate token for User A
            var tokenA = jwtService.GenerateAccessToken(userA);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

            // Act
            // User A tries to access Project B
            var response = await _client.GetAsync($"/api/project/{projectB.Id}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden, "Users must not access projects they aren't members of");
        }

        [Fact]
        public async Task CreateProject_ShouldRecordAuditLog_WhenSuccessful()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
            await db.Database.EnsureCreatedAsync();

            // 1. Create a global admin user
            var admin = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Audit Admin",
                Login = "audit_admin_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.ADMIN
            };
            db.Accounts.Add(admin);
            await db.SaveChangesAsync();

            // 2. Generate token and set headers
            var token = jwtService.GenerateAccessToken(admin);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            // 3. Prepare project request
            var request = new ProjectRequest
            {
                Name = "Audit Test Project",
                Description = "Testing audit trail"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/project", request);

            // Assert
            response.EnsureSuccessStatusCode();
            var createResponse = await response.Content.ReadFromJsonAsync<CreateProjectRequest>();
            createResponse.Should().NotBeNull();
            var projectId = createResponse!.ProjectId;

            // 4. Verify Audit Log
            using var verifyScope = _factory.Services.CreateScope();
            var verifyLogsRepo = verifyScope.ServiceProvider.GetRequiredService<LogsRepository>();
            
            var logsResult = await verifyLogsRepo.GetByProjectId(projectId, 10, 1);
            logsResult.Success.Should().BeTrue();
            logsResult.Data.Should().Contain(l => 
                l.LogType == LogType.CREATED && 
                l.LogFor == LogFor.PROJECT && 
                l.Name == "Project created" &&
                l.UserId == admin.Id
            );
        }
    }
}
