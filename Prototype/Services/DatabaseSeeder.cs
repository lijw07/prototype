using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using Prototype.Services;

namespace Prototype.Services;

public class DatabaseSeeder
{
    private readonly SentinelContext _context;
    private readonly PasswordEncryptionService _passwordService;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(SentinelContext context, PasswordEncryptionService passwordService, ILogger<DatabaseSeeder> logger)
    {
        _context = context;
        _passwordService = passwordService;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        try
        {
            _logger.LogInformation("Starting database seeding...");

            // Seed default admin user if no users exist
            await SeedDefaultAdminUser();

            // Seed sample data if in development
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                await SeedDevelopmentData();
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("Database seeding completed successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    private async Task SeedDefaultAdminUser()
    {
        // Check if any users exist
        var userExists = await _context.Users.AnyAsync();
        
        if (!userExists)
        {
            _logger.LogInformation("No users found. Creating default admin user...");
            
            var defaultAdmin = new UserModel
            {
                UserId = Guid.NewGuid(),
                FirstName = "System",
                LastName = "Administrator",
                Username = "admin",
                Email = "admin@prototype.local",
                PasswordHash = _passwordService.HashPassword("Admin123!"),
                PhoneNumber = "+1 (555) 000-0000",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(defaultAdmin);
            _logger.LogInformation("Default admin user created with username: admin and password: Admin123!");
        }
    }

    private async Task SeedDevelopmentData()
    {
        _logger.LogInformation("Seeding development data...");

        // Add sample users for development
        var existingUsers = await _context.Users.CountAsync();
        if (existingUsers <= 1) // Only admin exists
        {
            var sampleUsers = new[]
            {
                new UserModel
                {
                    UserId = Guid.NewGuid(),
                    FirstName = "John",
                    LastName = "Developer",
                    Username = "john.dev",
                    Email = "john.dev@prototype.local",
                    PasswordHash = _passwordService.HashPassword("Dev123!"),
                    PhoneNumber = "+1 (555) 123-4567",
                    CreatedAt = DateTime.UtcNow.AddDays(-30),
                    UpdatedAt = DateTime.UtcNow.AddDays(-30)
                },
                new UserModel
                {
                    UserId = Guid.NewGuid(),
                    FirstName = "Jane",
                    LastName = "Manager",
                    Username = "jane.manager",
                    Email = "jane.manager@prototype.local",
                    PasswordHash = _passwordService.HashPassword("Manager123!"),
                    PhoneNumber = "+1 (555) 987-6543",
                    CreatedAt = DateTime.UtcNow.AddDays(-15),
                    UpdatedAt = DateTime.UtcNow.AddDays(-15)
                }
            };

            _context.Users.AddRange(sampleUsers);
            _logger.LogInformation("Added {Count} sample users for development.", sampleUsers.Length);
        }

        // Add sample applications
        var existingApps = await _context.Applications.CountAsync();
        if (existingApps == 0)
        {
            var sampleApplications = new[]
            {
                new ApplicationModel
                {
                    ApplicationId = Guid.NewGuid(),
                    ApplicationName = "Development Database",
                    ApplicationDescription = "Local development SQL Server instance",
                    ApplicationDataSourceType = Prototype.Enum.DataSourceTypeEnum.MicrosoftSqlServer,
                    CreatedAt = DateTime.UtcNow.AddDays(-7),
                    UpdatedAt = DateTime.UtcNow.AddDays(-7)
                },
                new ApplicationModel
                {
                    ApplicationId = Guid.NewGuid(),
                    ApplicationName = "Staging Database",
                    ApplicationDescription = "Staging environment database",
                    ApplicationDataSourceType = Prototype.Enum.DataSourceTypeEnum.MicrosoftSqlServer,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                }
            };

            _context.Applications.AddRange(sampleApplications);
            _logger.LogInformation("Added {Count} sample applications for development.", sampleApplications.Length);
        }
    }
}