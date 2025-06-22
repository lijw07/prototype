using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Data.SqlClient;
using Prototype.Data;
using Prototype.Database;
using Prototype.Database.Interface;
using Prototype.Database.MicrosoftSQLServer;
using Prototype.Database.MySql;
using Prototype.Database.PostgreSql;
using Prototype.Database.MongoDb;
using Prototype.Database.Redis;
using Prototype.Database.Api;
using Prototype.Database.File;
using Prototype.Middleware;
using Prototype.POCO;
using Prototype.Services;
using Prototype.Services.Factory;
using Prototype.Services.Interfaces;
using Prototype.Utility;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services (Dependency Injection)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register CORS for development (open for local Docker/dev use)
if (builder.Environment.IsDevelopment())
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.AllowAnyOrigin()
                  .AllowAnyMethod()
                  .AllowAnyHeader();
        });
    });
}

// Build connection string from environment variables for Docker compatibility
var dbHost = Environment.GetEnvironmentVariable("DB_HOST") ?? "localhost";
var dbPort = Environment.GetEnvironmentVariable("DB_PORT") ?? "1433";
var dbName = Environment.GetEnvironmentVariable("DB_NAME") ?? "PrototypeDb";
var dbUser = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "YourStrong!Passw0rd";

var connectionString = $"Server={dbHost},{dbPort};Database={dbName};User={dbUser};Password={dbPassword};TrustServerCertificate=True;MultipleActiveResultSets=True";

builder.Services.AddDbContext<SentinelContext>(options =>
    options.UseSqlServer(connectionString));

// Bind SMTP Settings
builder.Services.Configure<SmtpSettingsPoco>(
    builder.Configuration.GetSection("Smtp"));

// Register Application Services
builder.Services.AddScoped<IEmailNotificationFactoryService, EmailNotificationFactoryService>();
builder.Services.AddScoped<IUserRecoveryRequestFactoryService, UserRecoveryFactoryService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, AuthenticatedUserAccessor>();
builder.Services.AddScoped<IApplicationFactoryService, ApplicationFactoryService>();
builder.Services.AddScoped<IUserApplicationFactoryService, UserApplicationFactoryService>();
builder.Services.AddScoped<IApplicationConnectionFactoryService, ApplicationConnectionFactoryService>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<PasswordEncryptionService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<IUserRoleService, UserRoleService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Add Memory Cache
builder.Services.AddMemoryCache();

// SQL Server connection strategies are now self-contained in SqlServerDatabaseStrategy

// Add Database Connection Strategies
builder.Services.AddScoped<IDatabaseConnectionStrategy, SqlServerDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, MySqlDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, PostgreSqlDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, MongoDbDatabaseStrategy>();
builder.Services.AddScoped<IDatabaseConnectionStrategy, RedisDatabaseStrategy>();

// Add API Connection Strategies
builder.Services.AddScoped<IApiConnectionStrategy, RestApiConnectionStrategy>();

// Add File Connection Strategies
builder.Services.AddScoped<IFileConnectionStrategy, CsvFileConnectionStrategy>();
builder.Services.AddScoped<IFileConnectionStrategy, JsonFileConnectionStrategy>();

// Add HttpClient for API connections
builder.Services.AddHttpClient<RestApiConnectionStrategy>();

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
            logger.LogInformation("Database initialization attempt {Attempt} of {MaxAttempts}...", i + 1, maxRetries);
            
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
                        logger.LogInformation("Database existence check completed.");
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
                logger.LogInformation("Database migrations applied successfully.");
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
                logger.LogInformation("Database connection verified successfully.");
                
                // Seed database with initial data
                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
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

// ENSURE THE BE DOESN'T CONNECT TO THE DB BEFORE IT STARTS
app.MapGet("/health", () => Results.Ok("Healthy!"));

app.MapControllers();

app.Run();