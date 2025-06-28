using Prototype.Enum;

namespace Prototype.DTOs;

public class ApplicationDto
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public Guid CreatedBy { get; set; }
    public List<ApplicationConnectionDto> Connections { get; set; } = new List<ApplicationConnectionDto>();
}

public class ApplicationConnectionDto
{
    public Guid ApplicationConnectionId { get; set; }
    public string ConnectionString { get; set; } = string.Empty;
    public string DatabaseStrategy { get; set; } = string.Empty;
    public string? Schema { get; set; }
    public string Port { get; set; } = string.Empty;
    public string Host { get; set; } = string.Empty;
}