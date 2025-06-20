using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.Database.Interface;
using Prototype.Database.MicrosoftSQLServer;
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
builder.Services.AddScoped<IConnectionStrategy, UserPasswordStrategy>();
builder.Services.AddScoped<IConnectionStrategy, NoAuthStrategy>();
builder.Services.AddScoped<IConnectionStrategy, AzureAdPasswordStrategy>();
builder.Services.AddScoped<IConnectionStrategy, AzureAdIntegratedStrategy>();
builder.Services.AddScoped<SqlServerConnectionStrategy>();
builder.Services.AddScoped<TransactionService>();
builder.Services.AddScoped<PasswordEncryptionService>();
builder.Services.AddScoped<ValidationService>();
builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();
builder.Services.AddScoped<IUserAccountService, UserAccountService>();
builder.Services.AddScoped<DatabaseSeeder>();

// Add HTTP Context Accessor
builder.Services.AddHttpContextAccessor();

// Add Memory Cache
builder.Services.AddMemoryCache();

// Add connection strategies with encryption service
builder.Services.AddScoped<UserPasswordStrategy>();
builder.Services.AddScoped<AzureAdPasswordStrategy>();
builder.Services.AddScoped<AzureAdIntegratedStrategy>();
builder.Services.AddScoped<NoAuthStrategy>();

builder.Services.AddScoped<SqlServerConnectionStrategy>(provider =>
{
    var strategies = new IConnectionStrategy[]
    {
        provider.GetRequiredService<UserPasswordStrategy>(),
        provider.GetRequiredService<AzureAdPasswordStrategy>(),
        provider.GetRequiredService<AzureAdIntegratedStrategy>(),
        provider.GetRequiredService<NoAuthStrategy>()
    };
    return new SqlServerConnectionStrategy(strategies);
});

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
    
    try
    {
        logger.LogInformation("Initializing database...");
        
        // Use EnsureCreated for simple database setup (alternative to migrations)
        var created = context.Database.EnsureCreated();
        if (created)
        {
            logger.LogInformation("Database 'PrototypeDb' created successfully with all tables.");
        }
        else
        {
            logger.LogInformation("Database 'PrototypeDb' already exists.");
        }
        
        // Test database connection
        var canConnect = context.Database.CanConnect();
        if (canConnect)
        {
            logger.LogInformation("Database connection verified successfully.");
            
            // Seed database with initial data
            var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync();
        }
        else
        {
            logger.LogError("Unable to connect to database.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while applying database migrations.");
        throw; // Re-throw to prevent application startup with database issues
    }
}

// Global exception handling (should be first)
app.UseMiddleware<GlobalExceptionMiddleware>();

// Enable CORS in development
if (app.Environment.IsDevelopment())
{
    app.UseCors("AllowAll");
}

// Rate limiting (before authentication)
app.UseMiddleware<RateLimitingMiddleware>();

// Standard middleware
app.UseAuthentication();
app.UseAuthorization();

// ENSURE THE BE DOESN'T CONNECT TO THE DB BEFORE IT STARTS
app.MapGet("/health", () => Results.Ok("Healthy!"));

app.MapControllers();

app.Run();