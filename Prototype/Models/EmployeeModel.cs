namespace Prototype.Models;

public class EmployeeModel
{
    public Guid EmployeeId { get; set; }
    public string Username {get; set;}
    public string Password {get; set;}
    public string Email {get; set;}
    public string FirstName {get; set;}
    public string LastName {get; set;}
    public Status status {get; set;}
    public DateTime CreatedAt {get; set;}
    public DateTime UpdatedAt {get; set;}
    public DateTime DeletedAt {get; set;}
}