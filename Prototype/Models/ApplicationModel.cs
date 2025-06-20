using System.ComponentModel.DataAnnotations;
using Prototype.Enum;

namespace Prototype.Models;

public class ApplicationModel
{
    [Key]
    public required Guid ApplicationId { get; set; }
    
    [Required]
    [StringLength(100)]
    public required string ApplicationName { get; set; }
    
    [StringLength(500)]
    public string? ApplicationDescription { get; set; }
    
    [Required]
    public required DataSourceTypeEnum ApplicationDataSourceType { get; set; }
    
    [Required]
    public required DateTime CreatedAt { get; set; }
    
    [Required]
    public required DateTime UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<ApplicationLogModel> ApplicationLogs { get; set; } = new List<ApplicationLogModel>();
    public virtual ICollection<UserApplicationModel> UserApplications { get; set; } = new List<UserApplicationModel>();
    public virtual ICollection<ApplicationConnectionModel> Connections { get; set; } = new List<ApplicationConnectionModel>();
}