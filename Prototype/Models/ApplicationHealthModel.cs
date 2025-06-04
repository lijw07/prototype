using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class ApplicationHealthModel
{
    [Key]
    public required Guid ApplicationHealthId { get; set; }
    
    [Required]
    public required DateTime LastCheckTimestamp { get; set; }
    
    [Required]
    public required StatusEnum Status { get; set; }
    
    [Required]
    public required TimeSpan ResponseTime { get; set; }
}