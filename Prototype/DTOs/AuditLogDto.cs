using Prototype.Enum;

namespace Prototype.DTOs;

public class AuditLogDto
{
    public Guid AuditLogId { get; set; }
    public Guid UserId { get; set; }
    public required string Username { get; set; }
    public required ActionTypeEnum ActionType { get; set; }
    public required string Metadata { get; set; }
    public required DateTime CreatedAt { get; set; }
}