using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    public required string ApplicationName { get; set; }
    
    [Required]
    public Guid PermissionId { get; set; }
    
    [Required]
    [ForeignKey(nameof(PermissionId))]
    public PermissionModel Permission { get; set; }
    
    public ApplicationConnectionModel ApplicationConnections { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}