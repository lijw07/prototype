using Prototype.DTOs.Cache;

namespace Prototype.Services.Interfaces;

public interface IDashboardService
{
    Task<DashboardStatsCacheDto> GetUserDashboardStatsAsync(Guid userId);
    Task<object> GetUserApplicationsAsync(Guid userId);
    Task<object> GetRecentUserActivityAsync(Guid userId, int limit = 10);
    Task<object> GetUserStatsSummaryAsync(Guid userId);
    Task<object> GetUserNotificationsAsync(Guid userId);
    Task<TimeSpan> CalculateUserSessionDuration(Guid userId);
    Task<int> GetUserApplicationCount(Guid userId);
    Task<DateTime?> GetLastLoginTime(Guid userId);
}