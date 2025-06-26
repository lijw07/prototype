namespace Prototype.DTOs.Cache;

public class UserDashboardCacheDto
{
    public int TotalApplications { get; set; }
    public int RecentActivityCount { get; set; }
    public List<object> RecentActivities { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public bool IsFresh => GeneratedAt > DateTime.UtcNow.AddMinutes(-5);
}