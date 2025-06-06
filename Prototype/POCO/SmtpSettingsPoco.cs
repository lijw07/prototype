namespace Prototype.POCO;

public class SmtpSettingsPoco
{
    public required string Host { get; set; }
    public required int Port { get; set; }
    public required string FromEmail { get; set; }
    public required string Username { get; set; }
    public required string Password { get; set; }
}