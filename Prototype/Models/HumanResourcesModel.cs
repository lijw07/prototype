namespace Prototype.Models;

public class HumanResourcesModel
{
    public Guid HumanResourceId { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Manager { get; set; }
    public string Department { get; set; }
    public Status Status { get; set; }
}