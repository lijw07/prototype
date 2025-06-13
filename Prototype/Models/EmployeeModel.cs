using System.ComponentModel.DataAnnotations;

namespace Prototype.Models;

public class EmployeeModel
{
    [Key]
    public Guid EmployeeId { get; set; }
    
    [Required]
    public string FirstName { get; set; }
    
    [Required]
    public string LastName { get; set; }
}