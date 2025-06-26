namespace Prototype.DTOs.Cache;

public class CacheWrapperDto<T>
{
    public Guid UserId { get; set; }
    public T Data { get; set; } = default!;
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public string Version { get; set; } = "1.0";
}