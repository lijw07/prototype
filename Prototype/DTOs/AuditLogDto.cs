public class AuditLogDto
{
    public Guid AuditLogId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public int ActionType { get; set; }
    public string Metadata { get; set; }
    public DateTime CreatedAt { get; set; }
}