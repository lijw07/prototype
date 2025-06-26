namespace Prototype.DTOs.Cache;

public class ApplicationCacheDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string ConnectionType { get; set; } = string.Empty;
    public DateTime? LastAccessed { get; set; }
    public bool IsActive { get; set; }
}