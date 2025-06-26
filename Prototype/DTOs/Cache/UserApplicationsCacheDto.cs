namespace Prototype.DTOs.Cache;

public class UserApplicationsCacheDto
{
    public List<ApplicationCacheDto> Applications { get; set; } = new();
    public DateTime GeneratedAt { get; set; }
}