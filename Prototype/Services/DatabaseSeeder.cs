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
        // Check if admin user exists
        var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        
        if (existingAdmin == null)
        {
            _logger.LogInformation("No admin user found. Creating default admin user...");
            
            var defaultAdmin = new UserModel
            {
                UserId = Guid.NewGuid(),
                FirstName = "System",
                LastName = "Administrator",
                Username = "admin",
                Email = "admin@prototype.local",
                PasswordHash = _passwordService.HashPassword("Admin123!"),
                PhoneNumber = "+1 (555) 000-0000",
                IsActive = true,
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
            };

            _context.Users.Add(defaultAdmin);
            _logger.LogInformation("Default admin user created with username: admin and password: Admin123!");
        }
        else
        {
            // Reset admin password to known value
            existingAdmin.PasswordHash = _passwordService.HashPassword("Admin123!");
            existingAdmin.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Admin user password reset to Admin123!");
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
                    IsActive = true,
                    Role = "User",
                    LastLogin = DateTime.UtcNow.AddDays(-2),
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
                    IsActive = true,
                    Role = "Manager",
                    LastLogin = DateTime.UtcNow.AddHours(-4),
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

        // Create connections and user-application relationships
        await SeedApplicationConnectionsAndUserRelationships();
    }

    private async Task SeedApplicationConnectionsAndUserRelationships()
    {
        _logger.LogInformation("Seeding application connections and user-application relationships...");
        
        // Check if UserApplication relationships already exist
        var existingRelationships = await _context.UserApplications.CountAsync();
        if (existingRelationships > 0)
        {
            _logger.LogInformation("User-application relationships already exist. Skipping seeding.");
            return;
        }

        // Get all users and applications
        var users = await _context.Users.ToListAsync();
        var applications = await _context.Applications.ToListAsync();

        if (!users.Any() || !applications.Any())
        {
            _logger.LogWarning("No users or applications found to create relationships.");
            return;
        }

        // Create default connections for applications that don't have them
        var applicationConnections = new List<ApplicationConnectionModel>();
        foreach (var application in applications)
        {
            var existingConnection = await _context.ApplicationConnections
                .FirstOrDefaultAsync(ac => ac.ApplicationId == application.ApplicationId);
            
            if (existingConnection == null)
            {
                var connection = new ApplicationConnectionModel
                {
                    ApplicationConnectionId = Guid.NewGuid(),
                    ApplicationId = application.ApplicationId,
                    Application = application,
                    Host = "localhost",
                    Port = "1433", 
                    DatabaseName = application.ApplicationName.Replace(" ", "_"),
                    Url = $"Server=localhost,1433;Database={application.ApplicationName.Replace(" ", "_")};Integrated Security=true;",
                    Username = "defaultuser",
                    Password = "defaultpass",
                    AuthenticationType = Prototype.Enum.AuthenticationTypeEnum.UserPassword,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                applicationConnections.Add(connection);
            }
        }

        if (applicationConnections.Any())
        {
            _context.ApplicationConnections.AddRange(applicationConnections);
            await _context.SaveChangesAsync(); // Save connections first
            _logger.LogInformation("Created {Count} default application connections.", applicationConnections.Count);
        }

        // Now create UserApplication relationships
        var userApplications = new List<UserApplicationModel>();
        
        foreach (var user in users)
        {
            foreach (var application in applications)
            {
                // Get the connection for this application
                var connection = await _context.ApplicationConnections
                    .FirstOrDefaultAsync(ac => ac.ApplicationId == application.ApplicationId);
                
                if (connection != null)
                {
                    var userApplication = new UserApplicationModel
                    {
                        UserApplicationId = Guid.NewGuid(),
                        UserId = user.UserId,
                        User = user,
                        ApplicationId = application.ApplicationId,
                        Application = application,
                        ApplicationConnectionId = connection.ApplicationConnectionId,
                        ApplicationConnection = connection,
                        CreatedAt = DateTime.UtcNow
                    };
                    
                    userApplications.Add(userApplication);
                }
            }
        }

        if (userApplications.Any())
        {
            _context.UserApplications.AddRange(userApplications);
            _logger.LogInformation("Created {Count} user-application relationships.", userApplications.Count);
        }
    }
}