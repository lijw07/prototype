namespace Prototype.Models;

public class ApplicationModel
{
    public Guid ApplicationId { get; set; }
    public string ApplicationName { get; set; }
    public ConnectionType ConnectionType { get; set; }
    public ConnectionDetail ConnectionDetail { get; set; }
    public Status Status { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}