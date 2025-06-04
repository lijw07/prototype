using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class ConnectionCredentialModel
{
    [Key]
    public required Guid ConnectionCredentialId { get; set; }
    
    [Required]
    public required CredentialTypeEnum CredentialType { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}