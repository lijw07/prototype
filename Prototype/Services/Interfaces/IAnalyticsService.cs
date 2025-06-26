using Prototype.DTOs.Cache;

namespace Prototype.Services.Interfaces;

public interface IAnalyticsService
{
    Task<AnalyticsMetricsCacheDto> GetAnalyticsOverviewAsync();
    Task<object> GetUserGrowthMetricsAsync(int days = 30);
    Task<object> GetApplicationMetricsAsync();
    Task<object> GetSecurityMetricsAsync(int days = 30);
    Task<object> GetSystemHealthMetricsAsync();
    Task<double> CalculateSecurityScore(int failedLogins, int successfulLogins, int securityEvents);
    Task<double> CalculateUserGrowthRate(int days = 30);
    Task<double> CalculateSystemHealthScore();
    Task<double> CalculateAverageUserSessions();
    Task<decimal> CalculateCostSavings(int totalUsers, int totalApplications);
    Task<double> CalculateProductivityGain(int totalUsers, double averageUserSessions);
}