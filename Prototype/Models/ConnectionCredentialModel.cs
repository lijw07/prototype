using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class ConnectionCredentialModel
{
    [Key]
    public Guid ConnectionCredentialId { get; set; }
    
    [Required]
    public CredentialTypeEnum CredentialType { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
}