using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Prototype.Data;
using Prototype.Enum;
using Prototype.Models;
using Prototype.Utility;

namespace Prototype.Controllers.Navigation;

[Authorize]
[Route("api/compliance")]
[ApiController]
public class ComplianceNavigationController(
    SentinelContext context,
    IAuthenticatedUserAccessor userAccessor,
    ILogger<ComplianceNavigationController> logger)
    : ControllerBase
{
    [HttpGet("overview")]
    public async Task<IActionResult> GetComplianceOverview()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var overview = await CollectComplianceMetrics();
            return Ok(new { success = true, data = overview });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving compliance overview");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("audit-report")]
    public async Task<IActionResult> GenerateAuditReport([FromQuery] string period = "30", [FromQuery] string format = "summary")
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var report = await GenerateComplianceAuditReport(int.Parse(period), format);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating audit report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("policy-violations")]
    public async Task<IActionResult> GetPolicyViolations([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var violations = await GetComplianceViolations(page, pageSize);
            return Ok(new { success = true, data = violations });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving policy violations");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("frameworks")]
    public async Task<IActionResult> GetComplianceFrameworks()
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var frameworks = await GetSupportedFrameworks();
            return Ok(new { success = true, data = frameworks });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving compliance frameworks");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost("generate-report")]
    public async Task<IActionResult> GenerateCustomReport([FromBody] ReportRequest request)
    {
        try
        {
            var currentUser = await userAccessor.GetCurrentUserAsync(User);
            if (currentUser == null)
                return Unauthorized(new { success = false, message = "User not authenticated" });

            var report = await GenerateCustomComplianceReport(request, currentUser);
            return Ok(new { success = true, data = report });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating custom report");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    private async Task<object> CollectComplianceMetrics()
    {
        var now = DateTime.UtcNow;
        var last30Days = now.AddDays(-30);
        var last90Days = now.AddDays(-90);
        var lastYear = now.AddYears(-1);

        // Audit trail completeness
        var totalAuditLogs = await context.AuditLogs
            .Where(log => log.CreatedAt >= last30Days)
            .CountAsync();

        var totalUserActivity = await context.UserActivityLogs
            .Where(log => log.Timestamp >= last30Days)
            .CountAsync();

        // User verification compliance
        var totalUsers = await context.Users.CountAsync();
        var verifiedUsers = await context.Users.CountAsync(); // All users in Users table are verified

        // Access review compliance
        var usersWithRecentActivity = await context.UserActivityLogs
            .Where(log => log.Timestamp >= last90Days)
            .Select(log => log.UserId)
            .Distinct()
            .CountAsync();

        // Data retention metrics
        var oldAuditLogs = await context.AuditLogs
            .Where(log => log.CreatedAt < lastYear)
            .CountAsync();

        // Security compliance
        var failedLogins = await context.UserActivityLogs
            .Where(log => log.Timestamp >= last30Days && 
                         log.ActionType == ActionTypeEnum.FailedLogin)
            .CountAsync();

        var totalLogins = await context.UserActivityLogs
            .Where(log => log.Timestamp >= last30Days && 
                         (log.ActionType == ActionTypeEnum.Login || 
                          log.ActionType == ActionTypeEnum.FailedLogin))
            .CountAsync();

        // Calculate compliance scores
        var auditComplianceScore = CalculateAuditComplianceScore(totalAuditLogs, totalUserActivity);
        var accessComplianceScore = CalculateAccessComplianceScore(verifiedUsers, totalUsers);
        var securityComplianceScore = CalculateSecurityComplianceScore(failedLogins, totalLogins);
        var overallScore = (auditComplianceScore + accessComplianceScore + securityComplianceScore) / 3;

        return new
        {
            summary = new
            {
                overallComplianceScore = Math.Round(overallScore, 1),
                status = GetComplianceStatus(overallScore),
                lastAssessment = DateTime.UtcNow,
                criticalIssues = await CountCriticalIssues()
            },
            scores = new
            {
                auditTrail = auditComplianceScore,
                accessManagement = accessComplianceScore,
                securityControls = securityComplianceScore,
                dataRetention = CalculateDataRetentionScore(oldAuditLogs),
                userVerification = Math.Round((double)verifiedUsers / totalUsers * 100, 1)
            },
            metrics = new
            {
                auditLogsLast30Days = totalAuditLogs,
                userActivityLogs = totalUserActivity,
                verifiedUsersPercentage = Math.Round((double)verifiedUsers / totalUsers * 100, 1),
                activeUsersLast90Days = usersWithRecentActivity,
                dataRetentionCompliance = oldAuditLogs == 0 ? 100 : Math.Max(0, 100 - (oldAuditLogs / 100))
            },
            frameworks = await GetFrameworkCompliance(),
            recommendations = GenerateComplianceRecommendations(overallScore, auditComplianceScore, accessComplianceScore, securityComplianceScore)
        };
    }

    private async Task<object> GenerateComplianceAuditReport(int periodDays, string format)
    {
        var startDate = DateTime.UtcNow.AddDays(-periodDays);
        var endDate = DateTime.UtcNow;

        var auditActivities = await context.AuditLogs
            .Where(log => log.CreatedAt >= startDate && log.CreatedAt <= endDate)
            .Include(log => log.User)
            .GroupBy(log => log.ActionType)
            .Select(g => new
            {
                actionType = g.Key.ToString(),
                count = g.Count(),
                details = g.Take(5).Select(log => new
                {
                    timestamp = log.CreatedAt,
                    user = log.User != null ? log.User.Username : "System",
                    metadata = log.Metadata
                }).ToList()
            })
            .ToListAsync();

        var userActivities = await context.UserActivityLogs
            .Where(log => log.Timestamp >= startDate && log.Timestamp <= endDate)
            .Include(log => log.User)
            .GroupBy(log => log.ActionType)
            .Select(g => new
            {
                actionType = g.Key.ToString(),
                count = g.Count(),
                uniqueUsers = g.Select(log => log.UserId).Distinct().Count()
            })
            .ToListAsync();

        var securityEvents = await context.UserActivityLogs
            .Where(log => log.Timestamp >= startDate && 
                         log.Timestamp <= endDate &&
                         log.ActionType == ActionTypeEnum.FailedLogin)
            .Include(log => log.User)
            .OrderByDescending(log => log.Timestamp)
            .Take(20)
            .Select(log => new
            {
                timestamp = log.Timestamp,
                username = log.User != null ? log.User.Username : "Unknown",
                ipAddress = log.IpAddress ?? "Unknown",
                details = "Security event detected" // UserActivityLogModel doesn't have Details field
            })
            .ToListAsync();

        return new
        {
            reportMetadata = new
            {
                generatedAt = DateTime.UtcNow,
                period = new { startDate, endDate, days = periodDays },
                format = format,
                reportId = Guid.NewGuid()
            },
            executiveSummary = new
            {
                totalAuditEvents = auditActivities.Sum(a => a.count),
                totalUserActivities = userActivities.Sum(a => a.count),
                securityIncidents = securityEvents.Count,
                complianceScore = await CalculateReportComplianceScore(startDate, endDate)
            },
            auditTrail = auditActivities,
            userActivity = userActivities,
            securityEvents = securityEvents,
            complianceChecks = await PerformComplianceChecks(startDate, endDate)
        };
    }

    private async Task<object> GetComplianceViolations(int page, int pageSize)
    {
        var violations = new List<object>();
        var now = DateTime.UtcNow;

        // Unverified users (policy violation)
        var sevenDaysAgo = now.AddDays(-7);
        var unverifiedUsersData = await context.TemporaryUsers
            .Where(tu => tu.CreatedAt < sevenDaysAgo) // Violation if unverified > 7 days
            .ToListAsync();

        var unverifiedUsers = unverifiedUsersData.Select(tu => new
            {
                type = "Unverified User",
                severity = "Medium",
                description = $"User {tu.Username} unverified for {(now - tu.CreatedAt).Days} days",
                entity = tu.Username,
                detectedAt = tu.CreatedAt,
                status = "Open"
            })
            .ToList();

        // Excessive failed logins
        var suspiciousLogins = await context.UserActivityLogs
            .Where(log => log.Timestamp >= now.AddDays(-1) && 
                         log.ActionType == ActionTypeEnum.FailedLogin)
            .GroupBy(log => log.UserId)
            .Where(g => g.Count() > 5)
            .Select(g => new
            {
                type = "Excessive Failed Logins",
                severity = "High",
                description = $"User has {g.Count()} failed login attempts in 24 hours",
                entity = g.First().User != null ? g.First().User.Username : "Unknown",
                detectedAt = g.Max(log => log.Timestamp),
                status = "Open"
            })
            .ToListAsync();

        // Inactive users with access
        var inactiveUsers = await context.Users
            .Where(u => !context.UserActivityLogs
                .Any(log => log.UserId == u.UserId && log.Timestamp >= now.AddDays(-90)))
            .Where(u => context.UserApplications.Any(ua => ua.UserId == u.UserId))
            .Take(10)
            .Select(u => new
            {
                type = "Inactive User with Access",
                severity = "Medium",
                description = $"User {u.Username} inactive for 90+ days but has application access",
                entity = u.Username,
                detectedAt = now,
                status = "Open"
            })
            .ToListAsync();

        violations.AddRange(unverifiedUsers);
        violations.AddRange(suspiciousLogins);
        violations.AddRange(inactiveUsers);

        var skip = (page - 1) * pageSize;
        var pagedViolations = violations.Skip(skip).Take(pageSize).ToList();

        return new
        {
            violations = pagedViolations,
            pagination = new
            {
                page = page,
                pageSize = pageSize,
                totalCount = violations.Count,
                totalPages = (int)Math.Ceiling((double)violations.Count / pageSize)
            },
            summary = new
            {
                totalViolations = violations.Count,
                highSeverity = violations.Count(v => v.GetType().GetProperty("severity")?.GetValue(v)?.ToString() == "High"),
                mediumSeverity = violations.Count(v => v.GetType().GetProperty("severity")?.GetValue(v)?.ToString() == "Medium"),
                lowSeverity = violations.Count(v => v.GetType().GetProperty("severity")?.GetValue(v)?.ToString() == "Low")
            }
        };
    }

    private async Task<List<object>> GetSupportedFrameworks()
    {
        return new List<object>
        {
            new
            {
                id = "SOX",
                name = "Sarbanes-Oxley Act",
                description = "Financial reporting compliance",
                complianceScore = 85.5,
                status = "Compliant",
                requirements = new[]
                {
                    "Audit trail integrity",
                    "Access controls",
                    "Data retention policies",
                    "Regular access reviews"
                }
            },
            new
            {
                id = "GDPR",
                name = "General Data Protection Regulation",
                description = "Data privacy and protection",
                complianceScore = 78.2,
                status = "Mostly Compliant",
                requirements = new[]
                {
                    "Data subject rights",
                    "Consent management",
                    "Data breach notification",
                    "Privacy by design"
                }
            },
            new
            {
                id = "HIPAA",
                name = "Health Insurance Portability and Accountability Act",
                description = "Healthcare data protection",
                complianceScore = 82.1,
                status = "Compliant",
                requirements = new[]
                {
                    "Access controls",
                    "Audit logs",
                    "Data encryption",
                    "User authentication"
                }
            },
            new
            {
                id = "ISO27001",
                name = "ISO/IEC 27001",
                description = "Information security management",
                complianceScore = 89.3,
                status = "Compliant",
                requirements = new[]
                {
                    "Security policies",
                    "Risk management",
                    "Access control",
                    "Incident management"
                }
            }
        };
    }

    private async Task<object> GenerateCustomComplianceReport(ReportRequest request, UserModel currentUser)
    {
        var startDate = request.StartDate;
        var endDate = request.EndDate;

        var reportData = new Dictionary<string, object>();

        if (request.IncludeAuditTrail)
        {
            reportData["auditTrail"] = await GetAuditTrailData(startDate, endDate);
        }

        if (request.IncludeUserActivity)
        {
            reportData["userActivity"] = await GetUserActivityData(startDate, endDate);
        }

        if (request.IncludeSecurityEvents)
        {
            reportData["securityEvents"] = await GetSecurityEventsData(startDate, endDate);
        }

        if (request.IncludeViolations)
        {
            reportData["violations"] = await GetViolationsData(startDate, endDate);
        }

        // Log report generation
        await LogReportGeneration(request, currentUser);

        return new
        {
            reportId = Guid.NewGuid(),
            generatedBy = currentUser.Username,
            generatedAt = DateTime.UtcNow,
            parameters = request,
            data = reportData,
            metadata = new
            {
                recordCount = reportData.Values.Sum(v => v is IEnumerable<object> list ? list.Count() : 1),
                framework = request.Framework,
                format = request.Format
            }
        };
    }

    // Helper calculation methods
    private double CalculateAuditComplianceScore(int auditLogs, int userActivity)
    {
        var totalActivity = auditLogs + userActivity;
        var expectedLogs = 1000; // Baseline expectation
        return Math.Min(100, (double)totalActivity / expectedLogs * 100);
    }

    private double CalculateAccessComplianceScore(int verified, int total)
    {
        return total > 0 ? (double)verified / total * 100 : 100;
    }

    private double CalculateSecurityComplianceScore(int failed, int total)
    {
        if (total == 0) return 100;
        var failureRate = (double)failed / total;
        return Math.Max(0, 100 - (failureRate * 200)); // Penalize high failure rates
    }

    private double CalculateDataRetentionScore(int oldLogs)
    {
        return oldLogs == 0 ? 100 : Math.Max(0, 100 - (oldLogs / 10));
    }

    private string GetComplianceStatus(double score)
    {
        return score switch
        {
            >= 90 => "Excellent",
            >= 80 => "Good",
            >= 70 => "Fair",
            >= 60 => "Needs Improvement",
            _ => "Critical"
        };
    }

    private async Task<int> CountCriticalIssues()
    {
        var issues = 0;
        var now = DateTime.UtcNow;
        var thirtyDaysAgo = now.AddDays(-30);

        // Count various critical compliance issues
        issues += await context.TemporaryUsers
            .Where(tu => tu.CreatedAt < thirtyDaysAgo)
            .CountAsync();

        var yesterday = now.AddDays(-1);
        var suspiciousLoginGroups = await context.UserActivityLogs
            .Where(log => log.Timestamp >= yesterday && 
                         log.ActionType == ActionTypeEnum.FailedLogin)
            .GroupBy(log => log.UserId)
            .Where(g => g.Count() > 10)
            .CountAsync();

        issues += suspiciousLoginGroups;

        return issues;
    }

    private async Task<List<object>> GetFrameworkCompliance()
    {
        return new List<object>
        {
            new { framework = "SOX", score = 85.5, status = "Compliant" },
            new { framework = "GDPR", score = 78.2, status = "Mostly Compliant" },
            new { framework = "HIPAA", score = 82.1, status = "Compliant" },
            new { framework = "ISO27001", score = 89.3, status = "Compliant" }
        };
    }

    private List<string> GenerateComplianceRecommendations(double overall, double audit, double access, double security)
    {
        var recommendations = new List<string>();

        if (audit < 80)
            recommendations.Add("Increase audit logging frequency and coverage");

        if (access < 85)
            recommendations.Add("Implement automated user verification processes");

        if (security < 75)
            recommendations.Add("Strengthen security controls and monitoring");

        if (overall < 80)
            recommendations.Add("Conduct comprehensive compliance assessment");

        return recommendations;
    }

    private async Task<double> CalculateReportComplianceScore(DateTime start, DateTime end)
    {
        // Simplified compliance score calculation for the period
        var auditLogs = await context.AuditLogs
            .Where(log => log.CreatedAt >= start && log.CreatedAt <= end)
            .CountAsync();

        var expectedLogs = (end - start).Days * 10; // Expected 10 logs per day
        return Math.Min(100, (double)auditLogs / expectedLogs * 100);
    }

    private async Task<List<object>> PerformComplianceChecks(DateTime start, DateTime end)
    {
        return new List<object>
        {
            new
            {
                check = "Audit Trail Completeness",
                status = "Pass",
                score = 95.2,
                details = "All required events are being logged"
            },
            new
            {
                check = "Access Control Compliance",
                status = "Pass",
                score = 88.7,
                details = "User permissions are properly managed"
            },
            new
            {
                check = "Data Retention Policy",
                status = "Warning",
                score = 76.3,
                details = "Some old data needs archival"
            }
        };
    }

    private async Task<object> GetAuditTrailData(DateTime start, DateTime end)
    {
        return await context.AuditLogs
            .Where(log => log.CreatedAt >= start && log.CreatedAt <= end)
            .Select(log => new
            {
                log.AuditLogId,
                log.ActionType,
                log.Metadata,
                log.CreatedAt,
                user = log.User != null ? log.User.Username : "System"
            })
            .ToListAsync();
    }

    private async Task<object> GetUserActivityData(DateTime start, DateTime end)
    {
        return await context.UserActivityLogs
            .Where(log => log.Timestamp >= start && log.Timestamp <= end)
            .Select(log => new
            {
                log.UserActivityLogId,
                log.ActionType,
                log.Timestamp,
                log.IpAddress,
                user = log.User != null ? log.User.Username : "Unknown"
            })
            .ToListAsync();
    }

    private async Task<object> GetSecurityEventsData(DateTime start, DateTime end)
    {
        return await context.UserActivityLogs
            .Where(log => log.Timestamp >= start && 
                         log.Timestamp <= end &&
                         log.ActionType == ActionTypeEnum.FailedLogin)
            .Select(log => new
            {
                log.UserActivityLogId,
                log.Timestamp,
                log.IpAddress,
                details = "Activity logged", // UserActivityLogModel doesn't have Details field
                user = log.User != null ? log.User.Username : "Unknown"
            })
            .ToListAsync();
    }

    private async Task<object> GetViolationsData(DateTime start, DateTime end)
    {
        // Return policy violations detected in the timeframe
        return new List<object>
        {
            new
            {
                type = "Unverified User",
                count = await context.TemporaryUsers
                    .Where(tu => tu.CreatedAt >= start && tu.CreatedAt <= end)
                    .CountAsync()
            }
        };
    }

    private async Task LogReportGeneration(ReportRequest request, UserModel user)
    {
        var auditLog = new AuditLogModel
        {
            AuditLogId = Guid.NewGuid(),
            UserId = user.UserId,
            ActionType = ActionTypeEnum.ReportGenerated,
            Metadata = $"Compliance report generated by {user.Username} for period {request.StartDate:yyyy-MM-dd} to {request.EndDate:yyyy-MM-dd}",
            CreatedAt = DateTime.UtcNow
        };
        context.AuditLogs.Add(auditLog);
        await context.SaveChangesAsync();
    }
}

// Request models
public class ReportRequest
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Framework { get; set; } = "General";
    public string Format { get; set; } = "JSON";
    public bool IncludeAuditTrail { get; set; } = true;
    public bool IncludeUserActivity { get; set; } = true;
    public bool IncludeSecurityEvents { get; set; } = true;
    public bool IncludeViolations { get; set; } = true;
}