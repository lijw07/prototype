using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.SqlServer.TransactSql.ScriptDom;

namespace Prototype.Models;

public class UserPermissionModel
{
    [Key]
    public required Guid UserPermissionId { get; set; }

    [Required]
    public required Guid UserId { get; set; }
    
    [Required]
    public required UserModel User { get; set; }

    [Required]
    public required Guid PermissionId { get; set; }
    
    [Required]
    public required PermissionModel Permission { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required string CreatedBy { get; set; }
}