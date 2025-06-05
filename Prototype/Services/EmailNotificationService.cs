using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Prototype.POCO;

namespace Prototype.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;

    public EmailNotificationService(IOptions<SmtpSettings> smtpOptions)
    {
        var smtp = smtpOptions.Value;
        _fromEmail = smtp.FromEmail;
        _smtpClient = new SmtpClient(smtp.Host)
        {
            Port = smtp.Port,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password),
            EnableSsl = true
        };
    }

    public async Task SendVerificationEmail(string recipientEmail, string verificationCode)
    {
        var subject = "Verify your account";
        var body = $"Hello,\n\nYour verification code is: {verificationCode}\n\nThank you!";
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendPasswordResetEmail(string recipientEmail, string resetLink)
    {
        var subject = "Password Reset Request";
        var body = $"Hello,\n\nClick the link below to reset your password:\n{resetLink}\n\nIf you didn’t request this, please ignore this email.";
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendUsernameEmail(string recipientEmail, string username)
    {
        var subject = "Username Request";
        var body = $"Hi,\n\nYour username is: {username}";
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendGenericNotification(string recipientEmail, string subject, string body)
    {
        await SendEmailAsync(recipientEmail, subject, body);
    }

    private async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        var mailMessage = new MailMessage(_fromEmail, recipientEmail)
        {
            Subject = subject,
            Body = body
        };

        await _smtpClient.SendMailAsync(mailMessage);
    }
}
