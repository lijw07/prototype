namespace Prototype.DTOs.Cache;

public class SystemHealthCacheDto
{
    public bool DatabaseHealth { get; set; }
    public bool ApplicationConnectionsHealthy { get; set; }
    public double MemoryUsagePercentage { get; set; }
    public double UptimeHours { get; set; }
    public DateTime LastChecked { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
}