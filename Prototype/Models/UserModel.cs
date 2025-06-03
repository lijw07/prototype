namespace Prototype.Models;

public class UserModel
{
    public Guid UserId { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string Manager { get; set; }
    public string Department { get; set; }
    public string JobTitle { get; set; }
    public Permission Permission { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public Status Status { get; set; }
    public ActiveDirectoryModel ActiveDirectory { get; set; }
    public HumanResourcesModel HumanResources { get; set; }
    public List<ApplicationModel> Applications { get; set; }
}