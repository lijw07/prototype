namespace Prototype.Models;

public class EmployeeModel
{
    private Guid EmployeeId { get; set; }
    private string Username {get; set;}
    private string Password {get; set;}
    private string Email {get; set;}
    private string FirstName {get; set;}
    private string LastName {get; set;}
    private DateTime CreatedAt {get; set;}
    private DateTime UpdatedAt {get; set;}
    private DateTime DeletedAt {get; set;}
}