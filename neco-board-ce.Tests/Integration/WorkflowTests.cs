using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Models.DTO.Request.Auth;
using neco_board_ce.Models.DTO.Request.Projects;
using neco_board_ce.Models.DTO.Response.Auth;
using neco_board_ce.Models.DTO.Response.Projects;
using neco_board_ce.Models.Entity;
using neco_board_ce.Models.Enums;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Tests.Infrastructure;
using Xunit;

namespace neco_board_ce.Tests.Integration
{
    public class WorkflowTests : IClassFixture<TestWebFactory>
    {
        private readonly TestWebFactory _factory;
        private readonly HttpClient _client;

        public WorkflowTests(TestWebFactory factory)
        {
            _factory = factory;
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task FullWorkflow_Register_Login_CreateProject_VerifyLogs()
        {
            // --- 0. Setup ---
            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            await db.Database.EnsureCreatedAsync();

            // Create a seed admin to allow registration of new users
            var seedAdmin = new Account
            {
                Id = Guid.NewGuid(),
                Name = "Seed Admin",
                Login = "seed_admin",
                Password = BCrypt.Net.BCrypt.HashPassword("admin_pass"),
                Role = WorkspaceRoles.ADMIN
            };
            db.Accounts.Add(seedAdmin);
            await db.SaveChangesAsync();

            // --- 1. Login as Admin to register a new user ---
            var adminLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest 
            { 
                Login = "seed_admin", 
                Password = "admin_pass" 
            });
            adminLoginResponse.EnsureSuccessStatusCode();
            var adminAuthData = await adminLoginResponse.Content.ReadFromJsonAsync<RefreshResponse>();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", adminAuthData!.AccessToken);

            // --- 2. Register New User ---
            var newUserLogin = "new_user_" + Guid.NewGuid();
            var newUserPass = "SecurePass123!";
            var registerRequest = new RegisterRequest
            {
                Name = "New Workflow User",
                Login = newUserLogin,
                Password = newUserPass,
                ConfirmPassword = newUserPass
            };
            var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", registerRequest);
            registerResponse.EnsureSuccessStatusCode();

            // --- 3. Login as New User ---
            // Clear Admin header
            _client.DefaultRequestHeaders.Authorization = null;
            var userLoginResponse = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest 
            { 
                Login = newUserLogin, 
                Password = newUserPass 
            });
            userLoginResponse.EnsureSuccessStatusCode();
            var userAuthData = await userLoginResponse.Content.ReadFromJsonAsync<RefreshResponse>();
            _client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userAuthData!.AccessToken);

            // --- 4. Create Project as New User ---
            var projectRequest = new ProjectRequest
            {
                Name = "Workflow Project",
                Description = "Created during E2E test"
            };
            var createProjectResponse = await _client.PostAsJsonAsync("/api/project", projectRequest);
            createProjectResponse.EnsureSuccessStatusCode();
            var projectData = await createProjectResponse.Content.ReadFromJsonAsync<CreateProjectRequest>();
            var projectId = projectData!.ProjectId;

            // --- 5. Verify Audit Logs ---
            // Use a fresh scope to check DB state
            using var verifyScope = _factory.Services.CreateScope();
            var verifyLogsRepo = verifyScope.ServiceProvider.GetRequiredService<LogsRepository>();
            var verifyDb = verifyScope.ServiceProvider.GetRequiredService<Data.AppDbContext>();
            
            // Check Project Log
            var logsResult = await verifyLogsRepo.GetByProjectId(projectId, 10, 1);
            logsResult.Success.Should().BeTrue();
            logsResult.Data.Should().Contain(l => 
                l.LogType == LogType.CREATED && 
                l.LogFor == LogFor.PROJECT && 
                l.Name == "Project created"
            );

            // Check if User was correctly assigned as OWNER in the project roles table
            var membership = await verifyDb.UserProjectRoles.FirstOrDefaultAsync(r => r.ProjectId == projectId);
            membership.Should().NotBeNull();
            membership!.Role.Should().Be(ProjectRole.OWNER);
        }
    }
}
