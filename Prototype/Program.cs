using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Prototype.Controllers.Settings;
using Prototype.Data;
using Prototype.POCO;
using Prototype.Services;
using Prototype.Services.Factory;
using Prototype.Services.Interfaces;
using Prototype.Utility;

var builder = WebApplication.CreateBuilder(args);

// 1. Configure Services (Dependency Injection)
builder.Services.AddControllers();
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
builder.Services.AddScoped<IEmailNotificationService, EmailNotificationFactoryService>();
builder.Services.AddScoped<IEntityCreationService, EntityCreationService>();
builder.Services.AddScoped<IUserFactoryService, UserFactoryService>();
builder.Services.AddScoped<IUserActivityLogFactoryService, UserActivityLogFactoryService>();
builder.Services.AddScoped<IAuditLogFactoryService, AuditLogFactoryService>();
builder.Services.AddScoped<IUserRecoveryRequestFactoryService, UserRecoveryFactoryService>();
builder.Services.AddScoped(typeof(IRepositoryService<>), typeof(RepositoryService<>));
builder.Services.AddScoped<IUnitOfWorkService, UnitOfWorkFactoryService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenFactoryService>();
builder.Services.AddScoped<IAuthenticatedUserAccessor, AuthenticatedUserAccessor>();
builder.Services.AddHttpClient();

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
        // Relative path works in Docker, no hardcoded host/port
        c.SwaggerEndpoint("v1/swagger.json", "Prototype API V1");
    });
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
    app.UseHttpsRedirection(); // Only redirect in production!
}

// JWT middleware
app.UseAuthentication();
app.UseAuthorization();

app.UseStaticFiles();

app.UseRouting();

app.MapControllers();

// Optional: If you want conventional MVC support (not needed for pure APIs)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller}/{action=Index}/{id?}");

// 4. Run Migrations at Startup (run *before* requests are handled)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SentinelContext>();
    db.Database.Migrate();
}

// 5. Run the Application
app.Run();