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

        #region 11.1.1 Admin Bypass

        [Fact]
        public async Task HasAccessToProject_ShouldReturnTrue_WhenUserIsGlobalAdmin()
        {
            var (db, accessCheck) = await GetRequiredServices();
            var adminUser = await CreateAccount(db, WorkspaceRoles.ADMIN);
            var project = await CreateProject(db, Guid.NewGuid()); // Owned by someone else

            var result = await accessCheck.HasAccessToProject(adminUser.Id, project.Id);
            result.Result.Should().BeTrue("Admins should have access to any project");
        }

        [Fact]
        public async Task HasAccessToColumn_ShouldReturnTrue_WhenUserIsGlobalAdmin()
        {
            var (db, accessCheck) = await GetRequiredServices();
            var adminUser = await CreateAccount(db, WorkspaceRoles.ADMIN);
            var project = await CreateProject(db, Guid.NewGuid());
            var column = new Column { Id = Guid.NewGuid(), Name = "Test", ProjectId = project.Id };
            db.Columns.Add(column);
            await db.SaveChangesAsync();

            var result = await accessCheck.HasAccessToColumn(adminUser.Id, column.Id, ProjectRole.MODERATOR);
            result.Result.Should().BeTrue("Admins should bypass role checks in columns");
        }

        #endregion

        #region 11.1.2 Role Hierarchy

        [Fact]
        public async Task HasAccessToProject_ShouldReturnTrue_WhenUserHasHigherRole()
        {
            var (db, accessCheck) = await GetRequiredServices();
            var user = await CreateAccount(db, WorkspaceRoles.USER);
            var project = await CreateProject(db, Guid.NewGuid());
            
            // Assign MODERATOR role to user in this project
            db.UserProjectRoles.Add(new UserProjectRole { UserId = user.Id, ProjectId = project.Id, Role = ProjectRole.MODERATOR });
            await db.SaveChangesAsync();

            // Check if MODERATOR has access to VIEWER-level action
            var result = await accessCheck.HasAccessToProject(user.Id, project.Id, ProjectRole.VIEWER);
            result.Result.Should().BeTrue("Moderator should have access to Viewer-level actions");
        }

        [Fact]
        public async Task HasAccessToProject_ShouldReturnFalse_WhenUserHasLowerRole()
        {
            var (db, accessCheck) = await GetRequiredServices();
            var user = await CreateAccount(db, WorkspaceRoles.USER);
            var project = await CreateProject(db, Guid.NewGuid());
            
            // Assign VIEWER role to user
            db.UserProjectRoles.Add(new UserProjectRole { UserId = user.Id, ProjectId = project.Id, Role = ProjectRole.VIEWER });
            await db.SaveChangesAsync();

            // Check if VIEWER has access to MODERATOR-level action
            var result = await accessCheck.HasAccessToProject(user.Id, project.Id, ProjectRole.MODERATOR);
            result.Result.Should().BeFalse("Viewer should NOT have access to Moderator-level actions");
        }

        #endregion

        #region 11.1.3 Cross-Project Guard

        [Fact]
        public async Task HasAccessToProject_ShouldReturnFalse_WhenUserInOtherProject()
        {
            var (db, accessCheck) = await GetRequiredServices();
            var user = await CreateAccount(db, WorkspaceRoles.USER);
            
            var projectA = await CreateProject(db, user.Id); // Member of A
            var projectB = await CreateProject(db, Guid.NewGuid()); // NOT member of B

            db.UserProjectRoles.Add(new UserProjectRole { UserId = user.Id, ProjectId = projectA.Id, Role = ProjectRole.OWNER });
            await db.SaveChangesAsync();

            var result = await accessCheck.HasAccessToProject(user.Id, projectB.Id);
            result.Result.Should().BeFalse("User in Project A should not have access to Project B");
        }

        #endregion

        #region Helpers

        private async Task<Account> CreateAccount(AppDbContext db, WorkspaceRoles role)
        {
            var account = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Test User",
                Login = "login_" + Guid.NewGuid(),
                Password = "hashed_password",
                Role = role
            };
            db.Accounts.Add(account);
            await db.SaveChangesAsync();
            return account;
        }

        private async Task<Project> CreateProject(AppDbContext db, Guid ownerId)
        {
            // Ensure owner exists to avoid FK error
            var owner = await db.Accounts.FindAsync(ownerId);
            if (owner == null)
            {
                owner = await CreateAccount(db, WorkspaceRoles.USER);
                ownerId = owner.Id;
            }

            var project = new Project
            {
                Id = Guid.NewGuid(),
                Name = "Test Project",
                OwnerId = ownerId
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return project;
        }

        #endregion
    }
}
