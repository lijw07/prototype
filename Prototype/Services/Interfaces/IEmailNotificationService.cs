namespace Prototype.Services.Interfaces;

/// <summary>
/// IEmailNotificationService Is responsible for sending emails to users.
/// </summary>
public interface IEmailNotificationService
{
    Task SendVerificationEmail(string recipientEmail, string token);
    Task SendAccountCreationEmail(string recipientEmail, string username);
    Task SendPasswordResetEmail(string recipientEmail, string token);
    Task SendUsernameEmail(string recipientEmail, string username);
    Task SendGenericNotification(string recipientEmail, string subject, string body);
    Task SendPasswordResetVerificationEmail(string userEmail);
}