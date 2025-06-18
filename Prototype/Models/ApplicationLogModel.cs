using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationLogModel
{
    [Key]
    public required Guid ApplicationLogId { get; set; }
    
    [Required]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public required ApplicationModel Application { get; set; }
    
    [Required]
    public required ActionTypeEnum ActionType { get; set; }
    
    [Required]
    public required string Metadata { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}