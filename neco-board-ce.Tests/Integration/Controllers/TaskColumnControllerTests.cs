using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.DTO.Request.Tasks;
using neco_board_ce.Models.DTO.Response.Messages;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration.Controllers
{
    public class TaskColumnControllerTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;
        private readonly HttpClient _client;

        public TaskColumnControllerTests(TestWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task MoveTaskToColumn_ShouldReturnBadRequest_WhenTargetColumnInDifferentProject()
        {
            // Arrange
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var jwtService = scope.ServiceProvider.GetRequiredService<JwtService>();
            await db.Database.EnsureCreatedAsync();

            // 1. Create User A (The Owner of Project A)
            var userA = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Project A Owner",
                Login = "user_a_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };

            // 2. Create Project A and Task T1
            var projectA = new Project { Id = Guid.NewGuid(), Name = "Project A", OwnerId = userA.Id };
            var columnA = new Column { Id = Guid.NewGuid(), Name = "Column A", ProjectId = projectA.Id, Queue = 1 };
            var taskT1 = new ColumnTask { Id = Guid.NewGuid(), Name = "Task T1", ColumnId = columnA.Id, OwnerId = userA.Id };

            // 3. Create Project B and Column B1 (Target for kidnapping)
            var ownerB = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Project B Owner",
                Login = "user_b_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };
            var projectB = new Project { Id = Guid.NewGuid(), Name = "Project B", OwnerId = ownerB.Id };
            var columnB = new Column { Id = Guid.NewGuid(), Name = "Column B", ProjectId = projectB.Id, Queue = 1 };

            db.Accounts.AddRange(userA, ownerB);
            db.Projects.AddRange(projectA, projectB);
            db.Columns.AddRange(columnA, columnB);
            db.ColumnTasks.Add(taskT1);
            
            // Link User A as OWNER in Project A
            db.UserProjectRoles.Add(new UserProjectRole { UserId = userA.Id, ProjectId = projectA.Id, Role = ProjectRole.OWNER });
            
            await db.SaveChangesAsync();

            // 4. Generate token for User A
            var tokenA = jwtService.GenerateAccessToken(userA);
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", tokenA);

            // 5. Prepare request: Move Task T1 (from Project A) to Column B (in Project B)
            var request = new EditTaskColumnRequest { ColumnId = columnB.Id };

            // Act
            var response = await _client.PatchAsJsonAsync($"/api/tasks/{taskT1.Id}/column", request);

            // Assert
            response.StatusCode.Should().Be(System.Net.HttpStatusCode.BadRequest, "Tasks cannot be moved to columns in different projects");
            var error = await response.Content.ReadFromJsonAsync<ErrorMessageResponse>();
            error!.Message.Should().Be("Column not found.");

            // 6. Verify task remained in the original column
            using var verifyScope = _factory.Services.CreateScope();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            var taskInDb = await verifyDb.ColumnTasks.FindAsync(taskT1.Id);
            taskInDb!.ColumnId.Should().Be(columnA.Id, "Task should not have changed columns");
        }
    }
}
