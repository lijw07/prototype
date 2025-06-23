using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Models;
using Prototype.Enum;
using Prototype.Utility;

namespace Prototype.Controllers;

[Authorize]
[Route("api/user-provisioning")]
[ApiController]
public class UserProvisioningController : ControllerBase
{
    private readonly SentinelContext _context;
    private readonly IAuthenticatedUserAccessor _userAccessor;
    private readonly ILogger<UserProvisioningController> _logger;

    public UserProvisioningController(
        SentinelContext context,
        IAuthenticatedUserAccessor userAccessor,
        ILogger<UserProvisioningController> logger)
    {
        _context = context;
        _userAccessor = userAccessor;
        _logger = logger;
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetProvisioningOverview()
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var overview = await CollectProvisioningMetrics();
            return Ok(new { success = true, data = overview });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provisioning overview");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("pending-requests")]
    public async Task<IActionResult> GetPendingRequests([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var pendingRequests = await GetPendingProvisioningRequests(page, pageSize);
            return Ok(new { success = true, data = pendingRequests });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving pending requests");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("auto-provision")]
    public async Task<IActionResult> AutoProvisionUsers([FromBody] AutoProvisionRequest request)
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var result = await ProcessAutoProvisioning(request, currentUser);
            return Ok(new { success = true, data = result });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing auto-provisioning");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("provisioning-templates")]
    public async Task<IActionResult> GetProvisioningTemplates()
    {
        try
        {
            var currentUser = await _userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var templates = await GetTemplates();
            return Ok(new { success = true, data = templates });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving provisioning templates");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task<object> CollectProvisioningMetrics()
    {
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);
        var last7Days = now.AddDays(-7);

        // User provisioning statistics
        var totalUsers = await _context.Users.CountAsync();
        var pendingUsers = await _context.TemporaryUsers.CountAsync();
        var recentlyProvisioned = await _context.Users
            .Where(u => u.CreatedAt >= last7Days)
            .CountAsync();

        // Application access requests
        var totalApplications = await _context.Applications.CountAsync();
        var usersWithAccess = await _context.UserApplications
            .Select(ua => ua.UserId)
            .Distinct()
            .CountAsync();

        // Role assignments
        var totalRoles = await _context.UserRoles.CountAsync();
        var usersWithRoles = await _context.Users
            .Where(u => !string.IsNullOrEmpty(u.Role))
            .CountAsync();

        // Provisioning efficiency metrics
        var avgProvisioningTime = CalculateAverageProvisioningTime();
        var autoProvisioningRate = CalculateAutoProvisioningRate();

        return new
        {
            summary = new
            {
                totalUsers = totalUsers,
                pendingUsers = pendingUsers,
                recentlyProvisioned = recentlyProvisioned,
                provisioningEfficiency = Math.Round(autoProvisioningRate, 1)
            },
            userMetrics = new
            {
                total = totalUsers,
                verified = totalUsers, // All users in Users table are verified
                pending = pendingUsers,
                accessGranted = usersWithAccess,
                rolesAssigned = usersWithRoles
            },
            applicationAccess = new
            {
                totalApplications = totalApplications,
                usersWithAccess = usersWithAccess,
                averageAppsPerUser = totalUsers > 0 ? Math.Round((double)usersWithAccess / totalUsers, 1) : 0,
                accessCoverage = totalUsers > 0 ? Math.Round((double)usersWithAccess / totalUsers * 100, 1) : 0
            },
            efficiency = new
            {
                avgProvisioningTime = avgProvisioningTime,
                autoProvisioningRate = autoProvisioningRate,
                pendingBacklog = pendingUsers,
                throughput = recentlyProvisioned
            },
            trends = await GetProvisioningTrends()
        };
    }

    private async Task<object> GetPendingProvisioningRequests(int page, int pageSize)
    {
        var skip = (page - 1) * pageSize;
        
        var pendingUsersData = await _context.TemporaryUsers
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

        var totalCount = await _context.TemporaryUsers.CountAsync();

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

        var eligibleUsers = await _context.TemporaryUsers
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

                _context.Users.Add(newUser);
                _context.TemporaryUsers.Remove(tempUser);

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
                _logger.LogError(ex, "Auto-provisioning failed for user {Username}", tempUser.Username);
            }
        }

        await _context.SaveChangesAsync();

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
        var roles = await _context.UserRoles
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
        var last6Months = DateTime.UtcNow.AddMonths(-6);
        
        var rawMonthlyData = await _context.Users
            .Where(u => u.CreatedAt >= last6Months)
            .GroupBy(u => new { u.CreatedAt.Year, u.CreatedAt.Month })
            .Select(g => new
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                provisioned = g.Count()
            })
            .OrderBy(x => x.Year).ThenBy(x => x.Month)
            .ToListAsync();

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
        _context.AuditLogs.Add(auditLog);
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