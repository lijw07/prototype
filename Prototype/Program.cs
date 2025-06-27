using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Microsoft.OpenApi.Models;
using Prototype.Data;
using Prototype.Database;
using Prototype.Database.Interface;
using Prototype.Database.MicrosoftSQLServer;
using Prototype.Database.MySql;
using Prototype.Database.PostgreSql;
using Prototype.Database.MongoDb;
using Prototype.Database.Redis;
using Prototype.Database.Oracle;
using Prototype.Database.MariaDb;
using Prototype.Database.Sqlite;
using Prototype.Database.Cassandra;
using Prototype.Database.ElasticSearch;
using Prototype.Database.Api;
using Prototype.Database.File;
using Prototype.Database.Cloud;
using Prototype.Middleware;
using Prototype.POCO;
using Prototype.Repositories;
using Prototype.Services;
using Prototype.Services.Factory;
using Prototype.Services.Interfaces;
using Prototype.Services.BulkUpload;
using Prototype.Services.BulkUpload.Mappers;
using Prototype.Services.Validators;
using Prototype.Utility;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services (Dependency Injection)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Configure JSON options for HTTP endpoints
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo 
    { 
        Title = "Prototype API", 
        Version = "v1",
        Description = "Centralized Application Management System API"
    });

    // Add JWT Bearer Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});

// Register CORS for development (open for local Docker/dev use)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(
                    "http://localhost:3000", 
                    "http://localhost:8080",
                    "http://127.0.0.1:3000",
                    "http://127.0.0.1:8080",
                    "http://0.0.0.0:8080")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials(); // Required for SignalR
        });
    });
}

// Build connection string from environment variables for Docker compatibility
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "PrototypeDb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong!Passw0rd";

var connectionString = $"Server={dbHost},{dbPort};Database={dbName};User={dbUser};Password={dbPassword};TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=300;Max Pool Size=200;Min Pool Size=10;Pooling=True;Command Timeout=300";

builder.Services.AddDbContext<SentinelContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(300); // 5 minutes timeout for bulk operations
        sqlOptions.EnableRetryOnFailure(maxRetryCount: 3, maxRetryDelay: TimeSpan.FromSeconds(30), null);
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.FirstWithoutOrderByAndFilterWarning)));

// Bind SMTP Settings
builder.Services.Configure<SmtpSettingsPoco>(
    builder.Configuration.GetSection("Smtp"));

// Register Application Services
builder.Services.AddScoped<IEmailNotificationFactoryService, EmailNotificationFactoryService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, AuthenticatedUserAccessor>();
builder.Services.AddScoped<IApplicationFactoryService, ApplicationFactoryService>();
builder.Services.AddScoped<IApplicationConnectionFactoryService, ApplicationConnectionFactoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<IPasswordEncryptionService, PasswordEncryptionService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
// UserContext handled by AuthenticatedUserAccessor
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<IDatabaseSeeder, DatabaseSeeder>();

// Register Common Services
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<INavigationService, NavigationService>();
builder.Services.AddScoped<IHttpContextParsingService, HttpContextParsingService>();
builder.Services.AddScoped<IPaginationService, PaginationService>();

// Register Repository Pattern
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register Validators
builder.Services.AddScoped<RegisterRequestValidator>();
builder.Services.AddScoped<LoginRequestValidator>();
// TODO: Add FileUploadValidator in Phase 2

// Register Bulk Upload Services
builder.Services.AddScoped<IFileParsingService, FileParsingService>();
builder.Services.AddScoped<IBulkValidationService, BulkValidationService>();
builder.Services.AddScoped<IBulkDataProcessingService, BulkDataProcessingService>();
builder.Services.AddScoped<IBulkUploadService, BulkUploadService>();
builder.Services.AddScoped<ITableDetectionService, TableDetectionService>();
builder.Services.AddScoped<ITableMappingService, TableMappingService>();
builder.Services.AddScoped<IProgressService, ProgressService>();
builder.Services.AddSingleton<IJobCancellationService, JobCancellationService>();
builder.Services.AddScoped<IFileQueueService, FileQueueService>();
builder.Services.AddScoped<IBulkInsertService, SqlServerBulkInsertService>();

// Register Table Mappers
builder.Services.AddScoped<UserTableMapper>();
builder.Services.AddScoped<ApplicationTableMapper>();
builder.Services.AddScoped<UserApplicationTableMapper>();
builder.Services.AddScoped<TemporaryUserTableMapper>();
builder.Services.AddScoped<UserRoleTableMapper>();

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add SignalR
builder.Services.AddSignalR();

// Add Database Connection Strategies
builder.Services.AddScoped<IDatabaseConnectionStrategy, SqlServerDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, MySqlDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, PostgreSqlDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, MongoDbDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, RedisDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, OracleDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, MariaDbDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, SqliteDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, CassandraDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, ElasticSearchDatabaseStrategy>();

// Add API Connection Strategies
builder.Services.AddScoped<IApiConnectionStrategy, RestApiConnectionStrategy>();
// builder.Services.AddScoped<IApiConnectionStrategy, GraphQLConnectionStrategy>(); // TODO: Implement GraphQLConnectionStrategy
builder.Services.AddScoped<IApiConnectionStrategy, SoapApiConnectionStrategy>();

// Add File Connection Strategies
builder.Services.AddScoped<IFileConnectionStrategy, CsvFileConnectionStrategy>();
builder.Services.AddScoped<IFileConnectionStrategy, JsonFileConnectionStrategy>();

// Add Cloud Storage Connection Strategies
builder.Services.AddScoped<IFileConnectionStrategy, AmazonS3ConnectionStrategy>();
builder.Services.AddScoped<IFileConnectionStrategy, AzureBlobStorageConnectionStrategy>();
builder.Services.AddScoped<IFileConnectionStrategy, GoogleCloudStorageConnectionStrategy>();

// Add HttpClient for API connections
builder.Services.AddHttpClient<RestApiConnectionStrategy>();
// builder.Services.AddHttpClient<GraphQLConnectionStrategy>(); // TODO: Implement GraphQLConnectionStrategy
builder.Services.AddHttpClient<SoapApiConnectionStrategy>();

// Add Database Connection Factory
builder.Services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                     builder.Configuration["JwtSettings:Key"];
        
        if (string.IsNullOrEmpty(jwtKey))
        {
            throw new InvalidOperationException("JWT secret key must be configured via JWT_SECRET_KEY environment variable or JwtSettings:Key configuration setting. This is required for security.");
        }
        var jwtIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER") ?? 
                        builder.Configuration["JwtSettings:Issuer"] ?? 
                        "PrototypeApp";
        var jwtAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") ?? 
                          builder.Configuration["JwtSettings:Audience"] ?? 
                          "PrototypeUsers";
        
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            RequireExpirationTime = true
        };
        
        // Configure JWT Bearer to read token from query string for SignalR
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                
                // If the request is for our SignalR hub...
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) &&
                    (path.StartsWithSegments("/progressHub")))
                {
                    // Read the token out of the query string
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Configuration
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

// Configure SMTP Settings
builder.Services.Configure<SmtpSettingsPoco>(
    builder.Configuration.GetSection("SmtpSettings"));

// Add middlewares in the right order
var app = builder.Build();

// Apply Entity Framework migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<SentinelContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    // Add retry logic for database initialization
    var maxRetries = 10;
    var retryDelay = TimeSpan.FromSeconds(5);
    
    for (int i = 0; i < maxRetries; i++)
    {
        try
        {
            logger.LogDebug("Database initialization attempt {Attempt} of {MaxAttempts}...", i + 1, maxRetries);
            
            // First ensure the database exists by creating it if necessary
            try
            {
                // Create database using a master connection
                var masterConnectionString = connectionString.Replace($"Database={dbName}", "Database=master");
                using (var connection = new SqlConnection(masterConnectionString))
                {
                    await connection.OpenAsync();
                    using (var command = connection.CreateCommand())
                    {
                        command.CommandText = $"IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = '{dbName}') CREATE DATABASE [{dbName}]";
                        await command.ExecuteNonQueryAsync();
                        logger.LogDebug("Database existence check completed.");
                    }
                }
            }
            catch (Exception dbCreateEx)
            {
                logger.LogWarning(dbCreateEx, "Could not create database using master connection. Will try EnsureCreated.");
            }
            
            // Apply migrations (this will create the database schema)
            // Note: Don't use EnsureCreated with migrations - they conflict!
            try
            {
                await context.Database.MigrateAsync();
                logger.LogDebug("Database migrations applied successfully.");
            }
            catch (Exception migrationEx)
            {
                logger.LogWarning(migrationEx, "Migration failed, possibly due to existing schema. Checking if database is accessible...");
                
                // If migration fails, just check if we can connect
                var isConnected = await context.Database.CanConnectAsync();
                if (!isConnected)
                {
                    throw new Exception("Cannot connect to database after migration failure", migrationEx);
                }
            }
            
            // Test database connection
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                logger.LogDebug("Database connection verified successfully.");
                
                // Seed database with initial data
                var seeder = scope.ServiceProvider.GetRequiredService<IDatabaseSeeder>();
                await seeder.SeedAsync();
                
                // Success - exit the retry loop
                break;
            }
            else
            {
                logger.LogError("Unable to connect to database.");
                throw new Exception("Database connection failed");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Database initialization attempt {Attempt} failed", i + 1);
            
            if (i == maxRetries - 1)
            {
                logger.LogError("All database initialization attempts failed. Application cannot start.");
                throw;
            }
            
            logger.LogInformation("Waiting {Delay} seconds before retry...", retryDelay.TotalSeconds);
            await Task.Delay(retryDelay);
        }
    }
}

// Add request logging for debugging (should be absolute first)
// Commented out to reduce console noise - uncomment if needed for debugging
/*
if (app.Environment.IsDevelopment())
{
    app.Use(async (context, next) =>
    {
        var logger = context.RequestServices.GetRequiredService<ILogger<Program>>();
        // Skip logging for webpack dev server WebSocket requests
        if (!context.Request.Path.StartsWithSegments("/ws"))
        {
            logger.LogInformation("Request: {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        await next();
    });
}
*/

// Global exception handling (should be first)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable Swagger in development
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prototype API V1");
        c.RoutePrefix = "swagger"; // Swagger UI at /swagger
    });
}

// Enable CORS in development (MUST be before UseRouting)
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

// Rate limiting (before authentication) - disabled in development
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<RateLimitingMiddleware>();
}

// Configure static files first
app.UseStaticFiles();

// Standard middleware
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// ENSURE THE BE DOESN'T CONNECT TO THE DB BEFORE IT STARTS
app.MapGet("/health", () => Results.Ok("Healthy!"));
app.MapGet("/test-bulk", () => Results.Ok("Bulk upload route test working!"));

app.MapControllers();

// Map SignalR Hub
app.MapHub<Prototype.Hubs.ProgressHub>("/progressHub");

// SPA fallback - serve React app for client-side routes
app.MapFallbackToFile("index.html");

app.Run();