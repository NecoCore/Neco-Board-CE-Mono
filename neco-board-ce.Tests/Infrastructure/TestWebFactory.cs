using System.Data.Common;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using neco_board_ce.Data;
using neco_board_ce.Data.Context;
using Microsoft.Extensions.Configuration;

namespace neco_board_ce.Tests.Infrastructure
{
    public class TestWebFactory : WebApplicationFactory<Program>
    {
        static TestWebFactory()
        {
            // Set environment variables at the very beginning of the test process
            // to ensure AppConfig.EnvOptions picks them up if it's not yet initialized.
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            Environment.SetEnvironmentVariable("JWT_SECRET", "test-secret-key-that-is-at-least-32-chars-long");
            Environment.SetEnvironmentVariable("JWT_ISSUER", "test-issuer");
            Environment.SetEnvironmentVariable("JWT_AUDIENCE", "test-audience");
            Environment.SetEnvironmentVariable("DATABASE_TYPE", "sqlite");
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment("Development");

            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                var dbContextDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbContextDescriptor != null) services.Remove(dbContextDescriptor);

                var dbContextImplementationDescriptor = services.SingleOrDefault(
                    d => d.ServiceType == typeof(AppDbContext));
                if (dbContextImplementationDescriptor != null) services.Remove(dbContextImplementationDescriptor);

                // Create and open a code-only SQLite connection
                var connection = new SqliteConnection("DataSource=:memory:");
                connection.Open();

                services.AddSingleton<DbConnection>(connection);

                // Add DbContext using the SQLite connection
                services.AddDbContext<AppDbContext, SqliteDbContext>((sp, options) =>
                {
                    options.UseSqlite(sp.GetRequiredService<DbConnection>());
                });
            });
        }
    }
}
