namespace Prototype.Models;

public class UserApplicationModel
{
    public Guid UserId { get; set; }
    public UserModel User { get; set; }

    public Guid ApplicationId { get; set; }
    public ApplicationModel Application { get; set; }
}