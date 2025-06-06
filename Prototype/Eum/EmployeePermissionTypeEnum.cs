namespace Prototype.Utility;

public enum EmployeePermissionTypeEnum
{
    // Full platform-wide access, can manage everything
    PlatformAdmin,

    // Can view, create, edit, and delete applications
    ApplicationAdmin,

    // Can manage data sources and connections
    DataSourceAdmin,

    // Can view logs and audit activity
    AuditViewer,

    // Can manage user accounts and permissions
    UserManager,

    // Can only view assigned applications and data
    ApplicationUser,

    // Can request access or submit issues
    Requester,

    // Read-only access to everything assigned
    ReadOnly
}