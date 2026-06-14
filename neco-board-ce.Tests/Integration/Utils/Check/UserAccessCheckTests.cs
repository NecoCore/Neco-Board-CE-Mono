using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Data;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Tests.Infrastructure;
using neco_board_ce.Utils.Check;
using Xunit;

namespace neco_board_ce.Tests.Integration.Utils.Check
{
    public class UserAccessCheckTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;

        public UserAccessCheckTests(TestWebFactory factory)
        {
            _factory = factory;
        }

        private async Task<(AppDbContext, UserAccessCheck)> GetRequiredServices()
        {
            var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var accessCheck = scope.ServiceProvider.GetRequiredService<UserAccessCheck>();
            
            await db.Database.EnsureCreatedAsync();
            return (db, accessCheck);
        }

        [Fact]
        public async Task HasAccessToProject_ShouldReturnTrue_WhenUserIsGlobalAdmin()
        {
            // Arrange
            var (db, accessCheck) = await GetRequiredServices();

            var adminUser = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Global Admin",
                Login = "admin_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.ADMIN
            };

            var ownerUser = new Account
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
                Name = "Secret Project",
                OwnerId = ownerUser.Id
            };

            await db.Accounts.AddAsync(ownerUser);
            await db.Accounts.AddAsync(adminUser);
            await db.Projects.AddAsync(project);
            await db.SaveChangesAsync();

            // Act
            // Admin is NOT a member of the project
            var result = await accessCheck.HasAccessToProject(adminUser.Id, project.Id);

            // Assert
            result.Result.Should().BeTrue("Global ADMINs should bypass membership checks");
        }

        [Fact]
        public async Task HasAccessToProject_ShouldReturnFalse_WhenRegularUserIsNotMember()
        {
            // Arrange
            var (db, accessCheck) = await GetRequiredServices();

            var regularUser = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Regular User",
                Login = "user_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = WorkspaceRoles.USER
            };

            var ownerUser = new Account
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
                Name = "Private Project",
                OwnerId = ownerUser.Id
            };

            await db.Accounts.AddAsync(ownerUser);
            await db.Accounts.AddAsync(regularUser);
            await db.Projects.AddAsync(project);
            await db.SaveChangesAsync();

            // Act
            var result = await accessCheck.HasAccessToProject(regularUser.Id, project.Id);

            // Assert
            result.Result.Should().BeFalse("Regular users should not have access to projects they don't belong to");
            result.Message.Should().Be("User doesn't have access in project");
        }
    }
}
