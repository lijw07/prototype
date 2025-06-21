using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models;

public class UserApplicationModel
{
    [Key]
    public required Guid UserApplicationId { get; set; }

    [Required]
    public required Guid UserId { get; set; }
    
    [ForeignKey(nameof(UserId))]
    public required UserModel User { get; set; }

    [Required]
    public required Guid ApplicationId { get; set; }
    
    [ForeignKey(nameof(ApplicationId))]
    public required ApplicationModel Application { get; set; }

    [Required]
    public required Guid ApplicationConnectionId { get; set; }
    
    [ForeignKey(nameof(ApplicationConnectionId))]
    public required ApplicationConnectionModel ApplicationConnection { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}