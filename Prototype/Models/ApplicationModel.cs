using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    public required string ApplicationName { get; set; }
    
    public ICollection<ApplicationLogModel> ApplicationLog { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }
}