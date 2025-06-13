using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Prototype.POCO;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class EmailNotificationFactoryService : IEmailNotificationService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;
    private readonly string _jwtKey;
    private readonly IJwtTokenService _jwtTokenService;

    public EmailNotificationFactoryService(
        IOptions<SmtpSettingsPoco> smtpOptions,
        IConfiguration config,
        IJwtTokenService jwtTokenService)
    {
        _jwtTokenService = jwtTokenService;

        var smtp = smtpOptions.Value;
        ValidateSmtpSettings(smtp);

        _fromEmail = smtp.FromEmail;
        _smtpClient = new SmtpClient(smtp.Host)
        {
            Port = smtp.Port,
            Credentials = new NetworkCredential(smtp.Username, smtp.Password),
            EnableSsl = true
        };

        _jwtKey = config["JwtSettings:Key"] ?? throw new InvalidOperationException("JwtSettings:Key is missing in configuration.");
    }

    public async Task SendVerificationEmail(string recipientEmail, string token)
    {
        var verificationLink = $"http://localhost:8080/verify?token={token}";
        var subject = "Verify your account";
        var body = GenerateEmailHtml("Verify Email", verificationLink);
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendPasswordResetEmail(string recipientEmail, string token)
    {
        var resetLink = $"http://localhost:8080/reset-password?token={token}";
        var subject = "Reset Your Password";
        var body = GenerateEmailHtml("Reset Password", resetLink);
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendAccountCreationEmail(string recipientEmail, string username)
    {
        var subject = "Welcome to the Platform – Account Created";
        var body = $@"
            <html>
              <body>
                <p>Hello {username},</p>
                <p>Your account has been successfully created. You can now log in using your credentials.</p>
                <p>If you have any questions, feel free to reach out to our support team.</p>
                <br/>
                <p>Thanks,<br/>The Team</p>
              </body>
            </html>";
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendUsernameEmail(string recipientEmail, string username)
    {
        var subject = "Here’s Your Username";
        var body = $@"
            <html>
              <body>
                <p>Hello,</p>
                <p>As requested, here is your username:</p>
                <p style='font-weight: bold; font-size: 18px;'>{username}</p>
                <p>If you didn’t request this, feel free to ignore this email.</p>
                <br/>
                <p>Cheers,<br/>The Team</p>
              </body>
            </html>";
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendGenericNotification(string recipientEmail, string subject, string body)
    {
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendPasswordResetVerificationEmail(string recipientEmail)
    {
        var subject = "Your Password Has Been Successfully Reset";
        var body = $@"
                <html>
                  <body>
                    <p>Hello,</p>
                    <p>We wanted to let you know that your password has been successfully reset.</p>
                    <p>If you did not initiate this change, please contact support immediately.</p>
                    <br/>
                    <p>Thank you,<br/>The Team</p>
                  </body>
                </html>";
        await SendEmailAsync(recipientEmail, subject, body);
    }

    private async Task SendEmailAsync(string recipientEmail, string subject, string body)
    {
        var mailMessage = new MailMessage(_fromEmail, recipientEmail)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        await _smtpClient.SendMailAsync(mailMessage);
    }

    private void ValidateSmtpSettings(SmtpSettingsPoco smtp)
    {
        if (string.IsNullOrWhiteSpace(smtp.Host))
            throw new ArgumentException("SMTP host is required.", nameof(smtp.Host));
        if (smtp.Port <= 0)
            throw new ArgumentOutOfRangeException(nameof(smtp.Port), "SMTP port must be greater than 0.");
        if (string.IsNullOrWhiteSpace(smtp.FromEmail))
            throw new ArgumentException("FromEmail is required.", nameof(smtp.FromEmail));
        if (string.IsNullOrWhiteSpace(smtp.Username))
            throw new ArgumentException("SMTP username is required.", nameof(smtp.Username));
        if (string.IsNullOrWhiteSpace(smtp.Password))
            throw new ArgumentException("SMTP password is required.", nameof(smtp.Password));
    }

    private string GenerateEmailHtml(string action, string link)
    {
        return $@"
            <html>
              <body>
                <p>Hello,</p>
                <p>We received a request to {action.ToLower()}. Click the button below to proceed:</p>
                <a href='{link}' 
                   style='display: inline-block; padding: 10px 20px; font-size: 16px;
                          color: #fff; background-color: #007BFF; text-decoration: none; border-radius: 5px;'>
                  {action}
                </a>
                <p>If the button doesn't work, you can also copy and paste the link into your browser:</p>
                <p>{link}</p>
              </body>
            </html>";
    }
}