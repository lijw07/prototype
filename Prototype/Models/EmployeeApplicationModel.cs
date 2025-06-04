namespace Prototype.Models;

public class EmployeeApplicationModel
{
    public Guid EmployeeId { get; set; }
    public EmployeeModel Employee { get; set; }

    public Guid ApplicationId { get; set; }
    public ApplicationModel Application { get; set; }
}