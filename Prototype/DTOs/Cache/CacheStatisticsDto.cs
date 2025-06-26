namespace Prototype.DTOs.Cache;

public class CacheStatisticsDto
{
    public double HitRatio { get; set; }
    public int TotalQueries { get; set; }
    public double AverageLatencyMs { get; set; }
    public double MaxLatencyMs { get; set; }
    public double MinLatencyMs { get; set; }
    public int TotalHits { get; set; }
    public int TotalMisses { get; set; }
    public Dictionary<string, int> TopMissedKeys { get; set; } = new();
}