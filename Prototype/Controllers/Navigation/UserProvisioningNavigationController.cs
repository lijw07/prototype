using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Authorize]
[Route("api/user-provisioning")]
[ApiController]
public class UserProvisioningNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ILogger<UserProvisioningNavigationController> logger)
    : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetProvisioningOverview()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var overview = await CollectProvisioningMetrics();
            return Ok(new { success = true, data = overview });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving provisioning overview");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("pending-requests")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var pendingRequests = await GetPendingProvisioningRequests(page, pageSize);
            return Ok(new { success = true, data = pendingRequests });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving pending requests");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("auto-provision")]
    public async Task<IActionResult> AutoProvisionUsers([FromBody] AutoProvisionRequest request)
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var result = await ProcessAutoProvisioning(request, currentUser);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing auto-provisioning");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("provisioning-templates")]
    public async Task<IActionResult> GetProvisioningTemplates()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var templates = await GetTemplates();
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving provisioning templates");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }


    private async Task<object> CollectProvisioningMetrics()
    {
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);
        var last7Days = now.AddDays(-7);

        try
        {
            // Set a longer timeout for these operations and run them in parallel
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            var cancellationToken = cancellationTokenSource.Token;

            // Run COUNT queries sequentially to avoid DbContext concurrency issues
            var totalUsers = await context.Users.CountAsync(cancellationToken);
            var pendingUsers = await context.TemporaryUsers.CountAsync(cancellationToken);
            var recentlyProvisioned = await context.Users
                .Where(u => u.CreatedAt >= last7Days)
                .CountAsync(cancellationToken);
            var totalApplications = await context.Applications.CountAsync(cancellationToken);
            var usersWithAccess = await context.UserApplications
                .Select(ua => ua.UserId)
                .Distinct()
                .CountAsync(cancellationToken);
            var totalRoles = await context.UserRoles.CountAsync(cancellationToken);
            var usersWithRoles = await context.Users
                .Where(u => !string.IsNullOrEmpty(u.Role))
                .CountAsync(cancellationToken);

            var metrics = new
            {
                TotalUsers = totalUsers,
                PendingUsers = pendingUsers,
                RecentlyProvisioned = recentlyProvisioned,
                TotalApplications = totalApplications,
                UsersWithAccess = usersWithAccess,
                TotalRoles = totalRoles,
                UsersWithRoles = usersWithRoles
            };

            // Provisioning efficiency metrics
            var avgProvisioningTime = CalculateAverageProvisioningTime();
            var autoProvisioningRate = CalculateAutoProvisioningRate();

            return new
            {
                summary = new
                {
                    totalUsers = metrics.TotalUsers,
                    pendingUsers = metrics.PendingUsers,
                    recentlyProvisioned = metrics.RecentlyProvisioned,
                    provisioningEfficiency = Math.Round(autoProvisioningRate, 1)
                },
                userMetrics = new
                {
                    total = metrics.TotalUsers,
                    verified = metrics.TotalUsers, // All users in Users table are verified
                    pending = metrics.PendingUsers,
                    accessGranted = metrics.UsersWithAccess,
                    rolesAssigned = metrics.UsersWithRoles
                },
                applicationAccess = new
                {
                    totalApplications = metrics.TotalApplications,
                    usersWithAccess = metrics.UsersWithAccess,
                    averageAppsPerUser = metrics.TotalUsers > 0 ? Math.Round((double)metrics.UsersWithAccess / metrics.TotalUsers, 1) : 0,
                    accessCoverage = metrics.TotalUsers > 0 ? Math.Round((double)metrics.UsersWithAccess / metrics.TotalUsers * 100, 1) : 0
                },
                efficiency = new
                {
                    avgProvisioningTime = avgProvisioningTime,
                    autoProvisioningRate = autoProvisioningRate,
                    pendingBacklog = metrics.PendingUsers,
                    throughput = metrics.RecentlyProvisioned
                },
                trends = await GetProvisioningTrends()
            };
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Provisioning metrics collection timed out, returning cached/estimated values");
            // Return estimated/cached values when timeout occurs
            return new
            {
                summary = new
                {
                    totalUsers = -1, // Indicates data unavailable
                    pendingUsers = 0,
                    recentlyProvisioned = 0,
                    provisioningEfficiency = 75.0
                },
                userMetrics = new
                {
                    total = -1,
                    verified = -1,
                    pending = 0,
                    accessGranted = 0,
                    rolesAssigned = 0
                },
                applicationAccess = new
                {
                    totalApplications = 0,
                    usersWithAccess = 0,
                    averageAppsPerUser = 0.0,
                    accessCoverage = 0.0
                },
                efficiency = new
                {
                    avgProvisioningTime = 48.0,
                    autoProvisioningRate = 75.0,
                    pendingBacklog = 0,
                    throughput = 0
                },
                trends = new
                {
                    monthlyProvisioning = new List<object>(),
                    trend = 0.0
                },
                dataStatus = "timeout" // Indicate that data is incomplete
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error collecting provisioning metrics");
            throw;
        }
    }

    private async Task<object> GetPendingProvisioningRequests(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        
        var pendingUsersData = await context.TemporaryUsers
            .OrderBy(tu => tu.CreatedAt)
            .Skip(skip)
            .Take(pageSize)
            .ToListAsync();

        var pendingUsers = pendingUsersData.Select(tu => new
            {
                tu.TemporaryUserId,
                tu.FirstName,
                tu.LastName,
                tu.Username,
                tu.Email,
                tu.PhoneNumber,
                Role = "User", // Default role
                tu.CreatedAt,
                DaysWaiting = (DateTime.UtcNow - tu.CreatedAt).Days,
                Priority = GetRequestPriority("User", tu.CreatedAt)
            })
            .ToList();

        var totalCount = await context.TemporaryUsers.CountAsync();

        return new
        {
            requests = pendingUsers,
            pagination = new
            {
                page = page,
                pageSize = pageSize,
                totalCount = totalCount,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            },
            metrics = new
            {
                averageWaitTime = pendingUsers.Any() ? pendingUsers.Average(u => u.DaysWaiting) : 0,
                oldestRequest = pendingUsers.Any() ? pendingUsers.Max(u => u.DaysWaiting) : 0,
                highPriorityCount = pendingUsers.Count(u => u.Priority == "High")
            }
        };
    }

    private async Task<object> ProcessAutoProvisioning(AutoProvisionRequest request, UserModel currentUser)
    {
        var processed = new List<object>();
        var errors = new List<string>();

        var now = DateTime.UtcNow;
        var cutoffDate = request.Criteria.MaxDaysWaiting.HasValue 
            ? now.AddDays(-request.Criteria.MaxDaysWaiting.Value)
            : DateTime.MinValue;

        var eligibleUsers = await context.TemporaryUsers
            .Where(tu => request.Criteria.MaxDaysWaiting == null || 
                        tu.CreatedAt >= cutoffDate)
            .Take(request.MaxUsers ?? 50)
            .ToListAsync();

        foreach (var tempUser in eligibleUsers)
        {
            try
            {
                var newUser = new UserModel
                {
                    UserId = Guid.NewGuid(),
                    FirstName = tempUser.FirstName,
                    LastName = tempUser.LastName,
                    Username = tempUser.Username,
                    Email = tempUser.Email,
                    PhoneNumber = tempUser.PhoneNumber,
                    Role = "User", // Default role
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    PasswordHash = tempUser.PasswordHash
                };

                context.Users.Add(newUser);
                context.TemporaryUsers.Remove(tempUser);

                processed.Add(new
                {
                    userId = newUser.UserId,
                    username = newUser.Username,
                    email = newUser.Email,
                    role = newUser.Role,
                    status = "Provisioned"
                });

                // Log the provisioning
                await LogProvisioningAction(newUser, currentUser, "Auto-Provisioned");
            }
            catch (Exception ex)
            {
                errors.Add($"Failed to provision {tempUser.Username}: {ex.Message}");
                logger.LogError(ex, "Auto-provisioning failed for user {Username}", tempUser.Username);
            }
        }

        await context.SaveChangesAsync();

        return new
        {
            processedCount = processed.Count,
            errorCount = errors.Count,
            processedUsers = processed,
            errors = errors,
            summary = $"Successfully provisioned {processed.Count} users with {errors.Count} errors"
        };
    }

    private async Task<List<object>> GetTemplates()
    {
        // Role-based templates
        var roles = await context.UserRoles
            .Select(r => r.Role)
            .Distinct()
            .ToListAsync();

        var templates = new List<object>();

        foreach (var role in roles)
        {
            templates.Add(new
            {
                id = Guid.NewGuid(),
                name = $"{role} - Standard Access",
                role = role,
                description = $"Standard application access template for {role} users",
                applications = new List<object>(),
                isDefault = role == "User",
                createdAt = DateTime.UtcNow
            });
        }

        return templates;
    }

    // Helper methods
    private double CalculateAverageProvisioningTime()
    {
        return Random.Shared.Next(24, 72); // Simulated: 1-3 days
    }

    private double CalculateAutoProvisioningRate()
    {
        return Random.Shared.Next(60, 85); // Simulated: 60-85% automation rate
    }

    private static string GetRequestPriority(string role, DateTime createdAt)
    {
        var daysWaiting = (DateTime.UtcNow - createdAt).Days;
        
        if (role.Contains("Admin") || daysWaiting > 7)
            return "High";
        if (daysWaiting > 3)
            return "Medium";
        return "Low";
    }

    private async Task<object> GetProvisioningTrends()
    {
        try
        {
            var last6Months = DateTime.UtcNow.AddMonths(-6);
            
            // Add timeout protection for trends query
            using var cancellationTokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            
            var rawMonthlyData = await context.Users
                .Where(u => u.CreatedAt >= last6Months)
                .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
                .Select(g => new
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    provisioned = g.Count()
                })
                .OrderBy(x => x.Year).ThenBy(x => x.Month)
                .ToListAsync(cancellationTokenSource.Token);

            var monthlyData = rawMonthlyData.Select(x => new
            {
                month = $"{x.Year}-{x.Month:D2}",
                provisioned = x.provisioned
            }).ToList();

            return new
            {
                monthlyProvisioning = monthlyData,
                trend = monthlyData.Count > 1 ? 
                    ((double)(monthlyData.Last().provisioned - monthlyData.First().provisioned) / monthlyData.First().provisioned * 100) : 0
            };
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Provisioning trends query timed out");
            return new
            {
                monthlyProvisioning = new List<object>(),
                trend = 0.0
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving provisioning trends");
            return new
            {
                monthlyProvisioning = new List<object>(),
                trend = 0.0
            };
        }
    }

    private async Task LogProvisioningAction(UserModel user, UserModel currentUser, string action)
    {
        var auditLog = new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = currentUser.UserId,
            User = null,
            ActionType = ActionTypeEnum.UserProvisioned,
            Metadata = $"{action}: User {user.Username} provisioned by {currentUser.Username}",
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);
    }

}

// Request models
public class AutoProvisionRequest
{
    public AutoProvisionCriteria Criteria { get; set; } = new();
    public int? MaxUsers { get; set; }
    public bool ApplyDefaultAccess { get; set; } = true;
}

public class AutoProvisionCriteria
{
    public string? RolePattern { get; set; }
    public int? MaxDaysWaiting { get; set; }
}

