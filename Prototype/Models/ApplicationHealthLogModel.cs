namespace Prototype.Models;

public class ApplicationHealthLogModel
{
    public Guid ApplicationId { get; set; }
    public ApplicationModel Application { get; set; }

    public Guid ApplicationHealthId { get; set; }
    public ApplicationHealthModel ApplicationHealth { get; set; }
}