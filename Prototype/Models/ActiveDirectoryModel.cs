namespace Prototype.Models;

public class ActiveDirectoryModel
{
    public Guid ActiveDirectoryId { get; set; }
    public string Email { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public Status Status { get; set; }
}