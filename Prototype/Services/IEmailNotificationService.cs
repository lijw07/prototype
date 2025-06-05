namespace Prototype.Services;

/// <summary>
/// Service responsible for sending email notifications.
/// The verification email service constructs the verification link internally.
/// </summary>
public interface IEmailNotificationService
{
    Task SendVerificationEmail(string recipientEmail, string verificationCode);
    Task SendAccountCreationEmail(string recipientEmail, string username);
    Task SendPasswordResetEmail(string recipientEmail, string resetLink);
    Task SendUsernameEmail(string recipientEmail, string username);
    Task SendGenericNotification(string recipientEmail, string subject, string body);
}