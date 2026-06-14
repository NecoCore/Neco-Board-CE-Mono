namespace neco_board_ce.Data
{
    public static class Constants
    {
        public static readonly string VERSION = "0.4.0-beta";
        public static readonly string PROJECT_NAME = "Neco Board CE";

        public static class Roles
        {
            public const string Admin = "ADMIN";
            public const string Owner = "OWNER";
        }

        public static class Auth
        {
            public const string RefreshTokenCookie = "refreshToken";
            public const string AccessTokenQueryParam = "access_token";
            public const string ClaimName = "name";
            public const string ClaimAvatar = "avatar";
            public const string Policy = "AuthPolicy";
            public const string BearerScheme = "Bearer";
        }

        public static class Cors
        {
            public const string DevPolicy = "DevCors";
            public const string ProdPolicy = "ProdCors";
            public const string DevOrigin = "http://localhost:5173";
        }

        public static class Storage
        {
            public const string FolderAvatars = "avatars";
            public const string FolderTasks = "tasks";
            public const string SubfolderAttachments = "attachments";
            public const string SubfolderImages = "images";
            public const string TypeLocal = "local";
            public const string TypeS3 = "s3";
        }

        public static class Database
        {
            public const string ProviderSqlite = "sqlite";
            public const string ProviderPostgres = "postgres";
            public const string ProviderMsSql = "mssql";
            public const string ProviderMySql = "mysql";
            public const string MigrationsTable = "__migrations";
            public const string DefaultSchema = "public";
            public const string DefaultName = "neco-board-ce";
        }

        public static class Docs
        {
            public const string RestPath = "/docs/rest";
            public const string SocketPath = "/docs/socket";
            public const string FullRawPath = "/docs/full/raw";
            public const string FullUiPath = "/docs/full";
        }

        public static class Admin
        {
            public const string DefaultUsername = "admin";
            public const string DefaultPassword = "admin123";
        }

        public static class SignalR
        {
            public const string HubPath = "/conn";
        }
    }
}
