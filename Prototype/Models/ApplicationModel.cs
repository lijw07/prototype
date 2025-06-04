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
    
    public required ICollection<EmployeeModel> Employee { get; set; }
    
    [Required]
    public required Guid ApplicationHealthId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationHealthId))]
    public required ApplicationHealthModel ApplicationHealth { get; set; }
    
    public required Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public required UserModel User { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}