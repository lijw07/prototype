namespace Prototype.Configuration;

public class ValidationConfiguration
{
    public int MaxUsernameLength { get; set; } = 50;
    public int MaxEmailLength { get; set; } = 254;
    public int MaxFirstNameLength { get; set; } = 100;
    public int MaxLastNameLength { get; set; } = 100;
    public int MaxPhoneLength { get; set; } = 20;
    
    public string EmailRegexPattern { get; set; } = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
    public string[] ValidRoles { get; set; }
}