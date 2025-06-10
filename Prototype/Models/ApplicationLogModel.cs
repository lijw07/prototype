using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationLogModel
{
    [Key]
    public Guid ApplicationLogId { get; set; }
    
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public ApplicationModel Application { get; set; }
    
    [Required]
    public string ConnectionInformation { get; set; }
    
    [Required]
    public ApplicationActionTypeEnum applicationActionType { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public float ResponseTime { get; set; }
    
    [Required]
    public DateTime CreatedAt { get; set; }
    
    [Required]
    public DateTime UpdatedAt { get; set; }
    
    [Required]
    public DateTime LastChecked { get; set; }
}