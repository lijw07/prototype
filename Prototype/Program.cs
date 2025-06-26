using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
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
using Prototype.Models;
using Prototype.POCO;
using Prototype.Services;
using Prototype.Services.Factory;
using Prototype.Services.Interfaces;
using Prototype.Services.BulkUpload;
using Prototype.Services.BulkUpload.Mappers;
using Prototype.Services.Common;
using Prototype.Repositories;
using Prototype.Configuration;
using Prototype.Utility;
using Prototype.Utility.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins("http://localhost:3000", "http://localhost:8080")
                  .AllowAnyMethod()
                  .AllowAnyHeader()
                  .AllowCredentials();
        });
    });
}
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "PrototypeDb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong!Passw0rd";

var connectionString = $"Server={dbHost},{dbPort};Database={dbName};User={dbUser};Password={dbPassword};TrustServerCertificate=True;MultipleActiveResultSets=True;Connection Timeout=300;Max Pool Size=200;Min Pool Size=10;Pooling=True;Command Timeout=300";

const int BulkOperationTimeoutSeconds = 300;
const int MaxRetryCount = 3;
const int MaxRetryDelaySeconds = 30;

builder.Services.AddDbContext<SentinelContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.CommandTimeout(BulkOperationTimeoutSeconds);
        sqlOptions.EnableRetryOnFailure(MaxRetryCount, TimeSpan.FromSeconds(MaxRetryDelaySeconds), null);
    })
    .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
    .EnableDetailedErrors(builder.Environment.IsDevelopment())
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.FirstWithoutOrderByAndFilterWarning)));

builder.Services.Configure<SmtpSettingsPoco>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddScoped<IEmailNotificationFactoryService, EmailNotificationFactoryService>();
builder.Services.AddScoped<IUserRecoveryRequestFactoryService, UserRecoveryFactoryService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, AuthenticatedUserAccessor>();
builder.Services.AddScoped<IApplicationFactoryService, ApplicationFactoryService>();
builder.Services.AddScoped<IUserApplicationFactoryService, UserApplicationFactoryService>();
builder.Services.AddScoped<IApplicationConnectionFactoryService, ApplicationConnectionFactoryService>();
builder.Services.AddScoped<ITransactionService, TransactionService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<PasswordEncryptionService>();
builder.Services.AddScoped<IValidationService, ValidationService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Register Bulk Upload Services
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

// Configure Redis Distributed Cache
var redisConnectionString = Environment.GetEnvironmentVariable("REDIS_CONNECTION_STRING") ?? 
                           "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = "CAMS";
});

// Register Redis connection for direct access
builder.Services.AddSingleton<StackExchange.Redis.IConnectionMultiplexer>(sp =>
{
    var configuration = StackExchange.Redis.ConfigurationOptions.Parse(redisConnectionString, true);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 3;
    configuration.ConnectTimeout = 5000;
    configuration.SyncTimeout = 2000;
    return StackExchange.Redis.ConnectionMultiplexer.Connect(configuration);
});

// Add Data Protection for cache encryption
builder.Services.AddDataProtection()
    .PersistKeysToStackExchangeRedis(sp => sp.GetService<StackExchange.Redis.IConnectionMultiplexer>()!)
    .SetApplicationName("CAMS");

// Register Caching Services
builder.Services.AddSingleton<ICacheMetricsService, CacheMetricsService>();
builder.Services.AddScoped<ICacheService, CacheService>();
builder.Services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
builder.Services.AddScoped<ICacheWarmupService, CacheWarmupService>();

// Configure settings
builder.Services.Configure<BulkUploadConfiguration>(builder.Configuration.GetSection(BulkUploadConfiguration.SectionName));

// Add common services for DRY optimization
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IAuditService, AuditService>();
builder.Services.AddScoped<IMappingService, MappingService>();
builder.Services.AddScoped<IErrorHandlerService, ErrorHandlerService>();
builder.Services.AddScoped<IRetryPolicyService, RetryPolicyService>();

// Add refactored user services for Single Responsibility Principle
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IUserActivityService, UserActivityService>();
builder.Services.AddScoped<IDeviceInformationService, DeviceInformationService>();
builder.Services.AddScoped<IUserAuthenticationService, UserAuthenticationService>();

builder.Services.AddScoped<IFileParsingService, FileParsingService>();
builder.Services.AddScoped<IProgressTrackingService, ProgressTrackingService>();
builder.Services.AddScoped<IBulkValidationService, BulkValidationService>();

// Add business logic services for Navigation controllers (SRP compliance)
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();

// Add specialized logging services (Clean Code - Extract logging concerns)
builder.Services.AddScoped<IBulkUploadLogger, BulkUploadLogger>();

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
builder.Services.AddHttpClient<SoapApiConnectionStrategy>();

// Add Database Connection Factory
builder.Services.AddScoped<IDatabaseConnectionFactory, DatabaseConnectionFactory>();

// Configure JWT Authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtKey = Environment.GetEnvironmentVariable("JWT_SECRET_KEY") ?? 
                     builder.Configuration["JwtSettings:Key"] ?? 
                     "your-super-secret-jwt-key-that-is-at-least-32-characters-long!";
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
            
            try
            {
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
            
            try
            {
                await context.Database.MigrateAsync();
                logger.LogDebug("Database migrations applied successfully.");
            }
            catch (Exception migrationEx)
            {
                logger.LogWarning(migrationEx, "Migration failed, possibly due to existing schema. Checking if database is accessible...");
                var isConnected = await context.Database.CanConnectAsync();
                if (!isConnected)
                {
                    throw new Exception("Cannot connect to database after migration failure", migrationEx);
                }
            }
            var canConnect = await context.Database.CanConnectAsync();
            if (canConnect)
            {
                logger.LogDebug("Database connection verified successfully.");
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();
                
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

// Enable CORS in development
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

// Rate limiting (before authentication) - disabled in development
if (!app.Environment.IsDevelopment())
{
    app.UseMiddleware<RateLimitingMiddleware>();
}

// Standard middleware
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/health", () => Results.Ok("Healthy!"));
app.MapGet("/test-bulk", () => Results.Ok("Bulk upload route test working!"));

app.MapControllers();

// Map SignalR Hub
app.MapHub<ProgressHub>("/progressHub");

app.Run();