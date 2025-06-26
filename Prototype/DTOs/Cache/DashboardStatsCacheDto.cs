namespace Prototype.DTOs.Cache;

public class DashboardStatsCacheDto
{
    public int TotalUsers { get; set; }
    public int TotalTemporaryUsers { get; set; }
    public int TotalRoles { get; set; }
    public int RecentActivityCount { get; set; }
    public int TotalApplications { get; set; }
    public DateTime GeneratedAt { get; set; }
    public bool IsFresh => GeneratedAt > DateTime.UtcNow.AddMinutes(-5);
}