namespace Prototype.Models;

public class UserModel
{
    private Guid UserId { get; set; }
    private string Username { get; set; }
    private string Password { get; set; }
    private string FirstName { get; set; }
    private string LastName { get; set; }
    private string Email { get; set; }
    private string Manager { get; set; }
    private string Department { get; set; }
    private string JobTitle { get; set; }
    private Permission Permission { get; set; }
    private DateTime CreatedAt { get; set; }
    private DateTime UpdatedAt { get; set; }
    private DateTime? DeletedAt { get; set; }
    private Status Status { get; set; }
}