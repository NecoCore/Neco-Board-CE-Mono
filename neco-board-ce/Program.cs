using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Interfaces;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Services.Realtime;
using neco_board_ce.Utils;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Docs;
using Saunter;
using Scalar.AspNetCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add env variables to configuration
builder.Configuration.AddInMemoryCollection(AppConfig.EnvOptions);

// Kestrel upload size limit (UPLOAD_MAX_FILE_SIZE, default 10 MB)
var maxFileSize = builder.Configuration.GetValue<long>("Storage:MaxFileSizeBytes", 10 * 1024 * 1024);
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = maxFileSize);

// CORS
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("DevCors", policy =>
        {
            policy.WithOrigins("http://localhost:5173")
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}
else
{
    var rawOrigins = builder.Configuration["App:AllowOrigins"];
    if (string.IsNullOrWhiteSpace(rawOrigins))
        throw new InvalidOperationException(
            "APP_ALLOW_ORIGINS must be set in production. " +
            "Provide a comma-separated list of allowed origins (e.g. https://app.example.com).");

    var allowedOrigins = rawOrigins.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("ProdCors", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials();
        });
    });
}

// Json serializer options to ignore cycles in entity relationships
builder.Services.AddControllers()
    .AddJsonOptions(o => {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Register SignalR (WebSocket)
builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.Converters
            .Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Register database context with dynamic provider based on configuration
AppConfig.GetDatabase(builder.Services, builder.Configuration);

// Register file storage service
AppConfig.AddFileStorage(builder.Services, builder.Configuration);

// Validate JWT config early — fail fast with a clear message instead of a late NRE.
var jwtSecret = builder.Configuration["Jwt:Secret"];
if (string.IsNullOrWhiteSpace(jwtSecret))
    throw new InvalidOperationException(
        "Jwt:Secret is not configured. Set the JWT_SECRET environment variable.");
if (System.Text.Encoding.UTF8.GetByteCount(jwtSecret) < 32)
    throw new InvalidOperationException(
        "Jwt:Secret must be at least 32 bytes (256 bits) long for HMAC-SHA256.");

// Jwt authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = builder.Configuration["Jwt:Issuer"] != null,
            ValidateAudience = builder.Configuration["Jwt:Audience"] != null,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtSecret))
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/conn"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

// Register repositories
builder.Services.AddScoped<AccountRepository>();
builder.Services.AddScoped<ProjectRepository>();
builder.Services.AddScoped<ColumnsRepository>();
builder.Services.AddScoped<ColumnTaskRepository>();
builder.Services.AddScoped<TaskUserRepository>();
builder.Services.AddScoped<UserProjectRoleRepository>();
builder.Services.AddScoped<TaskImagesRepository>();
builder.Services.AddScoped<TaskAttachmentsRepository>();
builder.Services.AddScoped<LogsRepository>();

// Register services
builder.Services.AddScoped<JwtService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<UserAccessCheck>();
builder.Services.AddSingleton<IRealtimeNotifier, RealtimeNotifier>();

// Global exception handling → RFC 7807 ProblemDetails
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddSingleton<Microsoft.AspNetCore.StaticFiles.FileExtensionContentTypeProvider>();

// Open API
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = Constants.PROJECT_NAME,
            Version = Constants.VERSION,
            Description = "Documentation for project management API"
        };

        document.Components ??= new();
        document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
        {
            ["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Enter JWT token"
            }
        };
        return Task.CompletedTask;
    });

    options.AddOperationTransformer((operation, context, ct) =>
    {
        var hasAuthorize = context.Description.ActionDescriptor.EndpointMetadata
            .OfType<AuthorizeAttribute>()
            .Any();

        if (hasAuthorize)
        {
            operation.Security = new List<OpenApiSecurityRequirement>
        {
            new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecuritySchemeReference("Bearer"),
                    new List<string>()
                }
            }
        };
        }

        return Task.CompletedTask;
    });

});

// AsyncAPI
builder.Services.AddAsyncApiSchemaGeneration(options =>
{
    // Tell Saunter which assembly to scan for [AsyncApi]-annotated types (AppSocketDocs).
    options.AssemblyMarkerTypes = new[] { typeof(AppSocketDocs) };

    // Required top-level "info" — without it the document is invalid and the UI cannot render.
    options.AsyncApi = new Saunter.AsyncApiSchema.v2.AsyncApiDocument
    {
        Info = new Saunter.AsyncApiSchema.v2.Info(Constants.PROJECT_NAME, Constants.VERSION)
        {
            Description = "Realtime SignalR events pushed by the server to clients."
        }
    };
});

// Ports
builder.WebHost.UseUrls(
    $"http://{builder.Configuration["App:Host"] ?? "*"}:{builder.Configuration["App:Port"] ?? "8080"}"
);

var app = builder.Build();


// Initialize db
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
    
    await AppConfig.InitializeDatabaseAsync(dbContext, config);
}


// Global exception handler — outermost middleware so it catches everything downstream.
app.UseExceptionHandler();

// Middleware
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference("/docs/rest", options =>
    {
        options.Title = "Board CE REST API";
    });

    app.MapAsyncApiDocuments();
    // Saunter's bundled UI (asyncapi-react 1.0.1) can't render the 2.4.0 document it generates,
    // so we serve our own page with a modern asyncapi-react from a CDN.
    app.MapGet("/docs/socket", () => Results.Content(AsyncApiUi.Html, "text/html"));

    app.MapGet("/docs/full/raw", async context =>
    {
        var xmlFileName = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFileName);

        if (File.Exists(xmlPath))
        {
            context.Response.ContentType = "application/xml";
            await context.Response.SendFileAsync(xmlPath);
        }
        else
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsync("XML documentation not found. Ensure <GenerateDocumentationFile>true</GenerateDocumentationFile> is set in .csproj");
        }
    });

    app.MapGet("/docs/full", () => Results.Content(DocsXmlUi.Html, "text/html"));
}
else
{
    app.UseHttpsRedirection();
}

app.UseRouting();
// CORS must sit between UseRouting and UseAuthentication.
app.UseCors(app.Environment.IsDevelopment() ? "DevCors" : "ProdCors");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<AppHub>("/conn");

app.Run();