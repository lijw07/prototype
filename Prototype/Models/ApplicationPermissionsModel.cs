namespace Prototype.Models;

public class ApplicationPermissionsModel
{
    public Guid ApplicationPermissionId { get; set; }
    public InternalPermission InternalPermission { get; set; }
    public ExternalPermission ExternalPermission { get; set; }
}