namespace Prototype.Models;

public class ApplicationPermissionsModel
{
    private Guid ApplicationPermissionId { get; set; }
    private InternalPermission InternalPermission { get; set; }
    private ExternalPermission ExternalPermission { get; set; }
}