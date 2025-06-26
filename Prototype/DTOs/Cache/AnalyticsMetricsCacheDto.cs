namespace Prototype.DTOs.Cache;

public class AnalyticsMetricsCacheDto
{
    public object Summary { get; set; } = new();
    public object UserMetrics { get; set; } = new();
    public object ApplicationMetrics { get; set; } = new();
    public object SecurityMetrics { get; set; } = new();
    public object OperationalMetrics { get; set; } = new();
    public object BusinessValue { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public bool IsFresh => GeneratedAt > DateTime.UtcNow.AddMinutes(-10);
}