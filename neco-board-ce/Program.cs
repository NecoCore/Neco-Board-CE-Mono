using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using neco_board_ce.Controllers.Hubs;
using neco_board_ce.Data;
using neco_board_ce.Repositories.Tables;
using neco_board_ce.Services.Authentication;
using neco_board_ce.Utils.Check;
using neco_board_ce.Utils.Docs;
using Saunter;
using Scalar.AspNetCore;

DotNetEnv.Env.Load();

var builder = WebApplication.CreateBuilder(args);

// Add env variables to configuration
builder.Configuration.AddInMemoryCollection(AppConfig.EnvOptions);

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
    if(!string.IsNullOrEmpty(builder.Configuration["App:AllowOrigins"]))
    {
        var allowedOrigins = builder.Configuration["App:AllowOrigins"]!.Split(',');
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
    else
    {
        builder.Services.AddCors(options =>
        {
            options.AddPolicy("ProdCors", policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyHeader()
                      .AllowAnyMethod()
                      .AllowCredentials();
            });
        });
    }
}

// Json serializer options to ignore cycles in entity relationships
builder.Services.AddControllers()
    .AddJsonOptions(o => {
        o.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        o.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });

// Register SignalR (WebSoket)
builder.Services.AddSignalR();
builder.Services.AddSignalRCore();

// Register database context with dynamic provider based on configuration
AppConfig.GetDatabase(builder.Services, builder.Configuration);

// Register file storage service
AppConfig.AddFileStorage(builder.Services, builder.Configuration);

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
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
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

// Open API
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "Neco Board CE",
            Version = "v0.2.1-beta",
            Description = "Documentation for project menagment API"
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
    options.Middleware.Route = "/docs/sockets/asyncapi.json";
    options.Middleware.UiBaseRoute = "/docs/socket/";
    options.Middleware.UiTitle = "Board CE sockets";
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


// Middleware
if (app.Environment.IsDevelopment())
{
    app.UseCors("DevCors");

    app.MapOpenApi();
    app.MapScalarApiReference("/docs/rest", options =>
    {
        options.Title = "Board CE REST API";
    });

    app.MapAsyncApiDocuments();
    app.MapAsyncApiUi();

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
    app.UseCors("ProdCors");
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.MapHub<ProjectHub>("/conn/projects");
app.MapHub<TaskHub>("/conn/task");
app.MapHub<AppHub>("/conn/app");

app.Run();