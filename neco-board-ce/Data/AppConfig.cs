using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using neco_board_ce.Data.Context;
using neco_board_ce.Interfaces;
using neco_board_ce.Services.Storage;

namespace neco_board_ce.Data
{
    public static class AppConfig
    {
        public static readonly Dictionary<string, string?> EnvOptions = new Dictionary<string, string?>
        {
            ["App:Port"] = Environment.GetEnvironmentVariable("APP_PORT"),
            ["App:Host"] = Environment.GetEnvironmentVariable("APP_HOST"),
            ["App:AllowOrigins"] = Environment.GetEnvironmentVariable("APP_ALLOW_ORIGINS"),

            ["Jwt:Secret"] = Environment.GetEnvironmentVariable("JWT_SECRET"),
            ["Jwt:Issuer"] = Environment.GetEnvironmentVariable("JWT_ISSUER"),
            ["Jwt:Audience"] = Environment.GetEnvironmentVariable("JWT_AUDIENCE"),
            ["Jwt:AccessTtl"] = Environment.GetEnvironmentVariable("JWT_ACCESS_TTL"),
            ["Jwt:RefreshTtl"] = Environment.GetEnvironmentVariable("JWT_REFRESH_TTL"),

            ["Storage:Type"] = Environment.GetEnvironmentVariable("FILE_STORAGE"),
            ["Storage:Local:BasePath"] = Environment.GetEnvironmentVariable("LOCAL_STORAGE_PATH"),
            ["Storage:S3:Region"] = Environment.GetEnvironmentVariable("S3_STORAGE_REGION"),
            ["Storage:S3:Bucket"] = Environment.GetEnvironmentVariable("S3_STORAGE_BUCKET"),
            ["Storage:S3:AccessKey"] = Environment.GetEnvironmentVariable("S3_STORAGE_ACCESS_KEY"),
            ["Storage:S3:SecretKey"] = Environment.GetEnvironmentVariable("S3_STORAGE_SECRET_KEY"),

            ["Database:Type"] = Environment.GetEnvironmentVariable("DATABASE_TYPE"),
            ["Database:Host"] = Environment.GetEnvironmentVariable("DATABASE_HOST"),
            ["Database:Port"] = Environment.GetEnvironmentVariable("DATABASE_PORT"),
            ["Database:Name"] = Environment.GetEnvironmentVariable("DATABASE_NAME"),
            ["Database:User"] = Environment.GetEnvironmentVariable("DATABASE_USER"),
            ["Database:Password"] = Environment.GetEnvironmentVariable("DATABASE_PASSWORD"),

            ["Admin:Username"] = Environment.GetEnvironmentVariable("ADMIN_USERNAME"),
            ["Admin:Password"] = Environment.GetEnvironmentVariable("ADMIN_PASSWORD"),
        };

        public static void GetDatabase(IServiceCollection services, IConfiguration config)
        {
            var dbType = config["Database:Type"]?.ToLower() ?? "sqlite";
            var dbHost = config["Database:Host"];
            var dbPort = config["Database:Port"];
            var dbName = config["Database:Name"] ?? "neco-board-ce";
            var dbUser = config["Database:User"];
            var dbPass = config["Database:Password"];

            switch (dbType)
            {
                case "postgres":
                    services.AddDbContext<AppDbContext, PostgresDbContext>(options => 
                        options.UseNpgsql(
                            $"Host={dbHost};Port={dbPort};Database={dbName};Username={dbUser};Password={dbPass};",
                            x => x.MigrationsAssembly("neco-board-ce")
                                    .MigrationsHistoryTable("__migrations", "public")));
                    break;

                case "mssql":
                    services.AddDbContext<AppDbContext, MsSqlDbContext>(options =>
                        options.UseSqlServer(
                            $"Server={dbHost},{dbPort};Database={dbName};User Id={dbUser};Password={dbPass};Encrypt=True;TrustServerCertificate=True;",
                            x => x.MigrationsAssembly("neco-board-ce")
                                    .MigrationsHistoryTable("__migrations")));
                    break;

                case "mysql":
                    services.AddDbContext<AppDbContext, MySqlDbContext>(options =>
                        options.UseMySQL(
                            $"Server={dbHost};Port={dbPort};Database={dbName};User={dbUser};Password={dbPass};",
                            x => x.MigrationsAssembly("neco-board-ce")
                                   .MigrationsHistoryTable("__migrations")));
                    break;

                case "sqlite":
                default:
                    services.AddDbContext<AppDbContext, SqliteDbContext>(options =>
                        options.UseSqlite(
                            $"Data Source={dbName}.db",
                            x => x.MigrationsAssembly("neco-board-ce")
                                   .MigrationsHistoryTable("__migrations")));
                    break;
            }
        }

        public static void AddFileStorage(IServiceCollection services, IConfiguration config)
        {
            var storageType = config["Storage:Type"]?.ToLower() ?? "local";

            switch (storageType)
            {
                case "s3":
                    services.AddSingleton<IFileStorage, S3FileStorage>();
                    break;
                case "local":
                default:
                    services.AddSingleton<IFileStorage, LocalFileStorage>();
                    break;
            }
        }

        public static async Task InitializeDatabaseAsync(AppDbContext db, IConfiguration config)
        {
            var dbType = config["Database:Type"]?.ToLower() ?? "sqlite";

            if (dbType == "sqlite")
                db.Database.EnsureCreated();
            else
                await db.Database.MigrateAsync();

            string adminLogin = config["Admin:Username"] ?? "admin";
            string adminPass = config["Admin:Password"] ?? "admin";

            if (!db.Accounts.Any(a => a.Login == adminLogin))
            {
                db.Accounts.Add(new neco_board_ce.Models.Entity.Account
                {
                    Name = "Admin",
                    Login = adminLogin,
                    Password = BCrypt.Net.BCrypt.HashPassword(adminPass),
                    Role = neco_board_ce.Models.Enums.WorkspaceRoles.OWNER
                });

                await db.SaveChangesAsync();
            }
        }
    }
}
