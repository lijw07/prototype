namespace Prototype.Models;

public class ApplicationModel
{
    private Guid ApplicationId { get; set; }
    private string ApplicationName { get; set; }
    private DateTime CreatedAt { get; set; }
    private DateTime UpdatedAt { get; set; }
    private DateTime DeletedAt { get; set; }
}