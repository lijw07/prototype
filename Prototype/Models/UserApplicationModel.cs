using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Prototype.Models;

public class UserApplicationModel
{
    [Key]
    public Guid UserApplicationId { get; set; }
    
    [Required]
    public Guid UserId { get; set; }
    
    [Required]
    [ForeignKey(nameof(UserId))]
    public UserModel User { get; set; }
    
    [Required]
    public Guid ApplicationId { get; set; }
    
    [Required]
    [ForeignKey(nameof(ApplicationId))]
    public ApplicationModel Application { get; set; }
}