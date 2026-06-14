using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.DTO.Request.Columns;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Controllers
{
    public class ColumnsProjectControllerTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;
        private readonly HttpClient _client;

        public ColumnsProjectControllerTests(TestWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task DeleteColumn_ShouldReturnForbidden_WhenUserIsNotMemberOfProject()
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

            // 2. Create User B (The Victim) and their Project + Column
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
                Name = "Victim's Project",
                OwnerId = userB.Id
            };

            var columnB = new Column
            {
                Id = Guid.NewGuid(),
                Name = "Important Column",
                ProjectId = projectB.Id,
                Queue = 1
            };

            db.Accounts.AddRange(userA, userB);
            db.Projects.Add(projectB);
            db.Columns.Add(columnB);
            
            // Link User B as OWNER in Project B
            db.UserProjectRoles.Add(new UserProjectRole { UserId = userB.Id, ProjectId = projectB.Id, Role = ProjectRole.OWNER });
            
            await db.SaveChangesAsync();

            // 3. Generate token for User A
            var tokenA = jwtService.GenerateAccessToken(userA);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

            // Act
            // User A tries to delete Column B
            var response = await _client.DeleteAsync($"/api/column/{columnB.Id}");

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.Forbidden, "Users must not delete columns in projects they don't belong to");
            
            // 4. Verify the column still exists in DB
            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            verifyDb.Columns.Any(c => c.Id == columnB.Id).Should().BeTrue("The column should not have been deleted");
        }

        [Fact]
        public async Task CreateColumn_ShouldSucceed_AndRecordLog_WhenUserIsOwner()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
            await db.Database.EnsureCreatedAsync();

            var user = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Project Owner",
                Login = "owner_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "My Project",
                OwnerId = user.Id
            };

            db.Accounts.Add(user);
            db.Projects.Add(project);
            db.UserProjectRoles.Add(new UserProjectRole { UserId = user.Id, ProjectId = project.Id, Role = ProjectRole.OWNER });
            await db.SaveChangesAsync();

            var token = jwtService.GenerateAccessToken(user);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            var request = new ColumnRequest { Name = "New Column", Color = "#FF0000" };

            // Act
            var response = await _client.PostAsJsonAsync($"/api/column/in-project/{project.Id}", request);

            // Assert
            response.EnsureSuccessStatusCode();
            
            // Verify DB and Logs
            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            verifyDb.Columns.Any(c => c.Name == "New Column" && c.ProjectId == project.Id).Should().BeTrue();
            verifyDb.Logs.Any(l => l.ProjectId == project.Id && l.Name == "Column created").Should().BeTrue();
        }
    }
}
