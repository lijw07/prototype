using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    public required string Name { get; set; }
    
    public required Guid DataSourceConnectionId { get; set; }
    
    [Required]
    [ForeignKey(nameof(DataSourceConnectionId))]
    public required DataSourceConnectionModel DataSourceConnection { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required ApplicationPermissionEnum ApplicationPermission { get; set; }
    
    public ICollection<EmployeeApplicationModel> EmployeeApplications { get; set; }
    
    public ICollection<ApplicationHealthLogModel> ApplicationHealthLog { get; set; }
    
    public ICollection<UserApplicationModel> UserApplications { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}