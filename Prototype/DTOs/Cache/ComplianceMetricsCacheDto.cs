namespace Prototype.DTOs.Cache;

public class ComplianceMetricsCacheDto
{
    public object Summary { get; set; } = new();
    public object Scores { get; set; } = new();
    public object Metrics { get; set; } = new();
    public object Frameworks { get; set; } = new();
    public object Recommendations { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
    public bool IsFresh => GeneratedAt > DateTime.UtcNow.AddMinutes(-15);
}