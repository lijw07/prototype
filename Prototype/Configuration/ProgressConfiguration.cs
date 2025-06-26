namespace Prototype.Configuration;

public class ProgressConfiguration
{
    public int UpdateIntervalRows { get; set; } = 100;
    public int MinProgressUpdateIntervalMs { get; set; } = 500;
    public int MaxProgressUpdateIntervalMs { get; set; } = 2000;
}