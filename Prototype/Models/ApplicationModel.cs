using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Utility;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public Guid ApplicationId { get; set; }
    
    [Required]
    public string Name { get; set; }
    
    public Guid DataSourceConnectionId { get; set; }
    
    [Required]
    [ForeignKey(nameof(DataSourceConnectionId))]
    public DataSourceConnectionModel DataSourceConnection { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public ApplicationPermissionEnum ApplicationPermission { get; set; }
    
    public ICollection<EmployeeModel> Employee { get; set; }
    
    [Required]
    public Guid ApplicationHealthId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationHealthId))]
    public ApplicationHealthModel ApplicationHealth { get; set; }
    
    public Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}