using System.Text;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Data;
using Prototype.Database.Interface;
using Prototype.Database.MicrosoftSQLServer;
using Prototype.POCO;
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

builder.Services.AddDbContext<SentinelContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Bind SMTP Settings
builder.Services.Configure<SmtpSettingsPoco>(
    builder.Configuration.GetSection("Smtp"));

// Register Application Services
builder.Services.AddScoped<IEmailNotificationFactoryService, EmailNotificationFactoryService>();
builder.Services.AddScoped<IEntityCreationFactoryService, EntityCreationFactoryService>();
builder.Services.AddScoped<IUserFactoryService, UserFactoryService>();
builder.Services.AddScoped<IUserActivityLogFactoryService, UserActivityLogFactoryService>();
builder.Services.AddScoped<IAuditLogFactoryService, AuditLogFactoryService>();
builder.Services.AddScoped<IUserRecoveryRequestFactoryService, UserRecoveryFactoryService>();
builder.Services.AddScoped(typeof(IRepositoryFactoryService<>), typeof(RepositoryFactoryService<>));
builder.Services.AddScoped<IUnitOfWorkFactoryService, UnitOfWorkFactoryService>();
builder.Services.AddScoped<IJwtTokenFactoryService, JwtTokenFactoryFactoryService>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, AuthenticatedUserAccessor>();
builder.Services.AddScoped<IApplicationFactoryService, ApplicationFactoryService>();
builder.Services.AddScoped<IApplicationLogFactoryService, ApplicationLogFactoryService>();
builder.Services.AddScoped<IUserApplicationFactoryService, UserApplicationFactoryService>();
builder.Services.AddSingleton<IConnectionStrategy, UserPasswordStrategy>();
builder.Services.AddSingleton<IConnectionStrategy, NoAuthStrategy>();
builder.Services.AddSingleton<IConnectionStrategy, AzureAdPasswordStrategy>();
builder.Services.AddSingleton<IConnectionStrategy, AzureAdIntegratedStrategy>();
builder.Services.AddSingleton<SqlServerConnectionStrategy>();


var jwtSettings = builder.Configuration.GetSection("JwtSettings");

builder.Services.AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidAudience = jwtSettings["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtSettings["Key"]!))
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

// 2. Configure Middleware Pipeline

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
    app.UseCors("AllowAll");
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Prototype API V1");
        c.RoutePrefix = "swagger";
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();

// JWT middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapControllers();

// Optional: If you want conventional MVC support (not needed for pure APIs)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

// 4. Run Migrations at Startup (run *before* requests are handled)
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var db = scope.ServiceProvider.GetRequiredService<SentinelContext>();
        db.Database.Migrate();
        logger.LogInformation("Database migration completed successfully.");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while migrating or initializing the database.");
        throw;
    }
}

// ENSURE THE BE DOESNT CONNECT TO THE DB BEFORE IT STARTS
app.MapGet("/health", () => Results.Ok("Healthy!"));

// 5. Run the Application
app.Run();