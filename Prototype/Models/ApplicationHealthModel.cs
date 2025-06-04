using System.ComponentModel.DataAnnotations;
using Prototype.Utility;

namespace Prototype.Models;

public class ApplicationHealthModel
{
    [Key]
    public Guid ApplicationHealthId { get; set; }
    
    [Required]
    public DateTime LastCheckTimestamp { get; set; }
    
    [Required]
    public StatusEnum Status { get; set; }
    
    [Required]
    public TimeSpan ResponseTime { get; set; }
}