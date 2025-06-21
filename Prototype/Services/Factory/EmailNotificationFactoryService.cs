using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;
using Prototype.POCO;
using Prototype.Services.Interfaces;

namespace Prototype.Services.Factory;

public class EmailNotificationFactoryService : IEmailNotificationFactoryService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;
    private readonly string _frontendBaseUrl;
    private readonly ILogger<EmailNotificationFactoryService> _logger;

    public EmailNotificationFactoryService(
        IOptions<SmtpSettingsPoco> smtpOptions,
        IConfiguration config,
        ILogger<EmailNotificationFactoryService> logger)
    {
        _logger = logger;

        var smtp = smtpOptions.Value;
        ValidateSmtpSettings(smtp);

        _fromEmail = smtp.FromEmail;
        _smtpClient = new SmtpClient(smtp.Host)
        {
            Port = smtp.Port,
            EnableSsl = true
        };

        // Only set credentials if provided (for production)
        if (!string.IsNullOrWhiteSpace(smtp.Username) && !string.IsNullOrWhiteSpace(smtp.Password))
        {
            _smtpClient.Credentials = new NetworkCredential(smtp.Username, smtp.Password);
        }
        else
        {
            // In development, log that we're running without SMTP credentials
            var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
            if (isDevelopment)
            {
                _logger.LogInformation("DEVELOPMENT MODE: SMTP credentials not configured. Emails will be logged instead of sent.");
            }
        }

        _frontendBaseUrl = config["Frontend:BaseUrl"] ?? "http://localhost:3000";
    }

    public async Task SendVerificationEmail(string recipientEmail, string token)
    {
        var verificationLink = $"{_frontendBaseUrl}/verify?token={token}";
        var subject = "Verify your account";
        var body = GenerateEmailHtml("Verify Email", verificationLink);
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendVerificationEmailAsync(string recipientEmail, string token)
    {
        await SendVerificationEmail(recipientEmail, token);
    }

    public async Task SendPasswordResetEmail(string recipientEmail, string token)
    {
        var resetLink = $"{_frontendBaseUrl}/reset-password?token={token}";
        var subject = "Reset Your Password";
        var body = GenerateEmailHtml("Reset Password", resetLink);
        await SendEmailAsync(recipientEmail, subject, body);
    }

    public async Task SendPasswordResetEmailAsync(string recipientEmail, string token)
    {
        await SendPasswordResetEmail(recipientEmail, token);
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
        var subject = "Here's Your Username";
        var body = $@"
            <html>
              <body>
                <p>Hello,</p>
                <p>As requested, here is your username:</p>
                <p style='font-weight: bold; font-size: 18px;'>{username}</p>
                <p>If you didn't request this, feel free to ignore this email.</p>
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
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        
        // In development without proper SMTP credentials, just log the email instead of sending
        if (isDevelopment && _smtpClient.Credentials == null)
        {
            _logger.LogInformation("DEVELOPMENT: Email would be sent to {Email} with subject: {Subject}", recipientEmail, subject);
            _logger.LogDebug("Email body: {Body}", body);
            return;
        }

        try
        {
            var mailMessage = new MailMessage(_fromEmail, recipientEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };

            await _smtpClient.SendMailAsync(mailMessage);
            _logger.LogInformation("Email sent successfully to {Email} with subject: {Subject}", recipientEmail, subject);
        }
        catch (SmtpFailedRecipientException ex)
        {
            _logger.LogError(ex, "Failed to send email to recipient: {Email}", ex.FailedRecipient);
            
            // In development, don't fail the entire operation due to email issues
            if (!isDevelopment)
                throw;
        }
        catch (SmtpException ex)
        {
            _logger.LogError(ex, "SMTP error occurred while sending email to {Email}: {StatusCode}", recipientEmail, ex.StatusCode);
            
            // In development, don't fail the entire operation due to email issues
            if (!isDevelopment)
                throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error occurred while sending email to {Email}", recipientEmail);
            
            // In development, don't fail the entire operation due to email issues
            if (!isDevelopment)
                throw;
        }
    }

    private static void ValidateSmtpSettings(SmtpSettingsPoco smtp)
    {
        if (string.IsNullOrWhiteSpace(smtp.Host))
            throw new ArgumentException("SMTP host is required.", nameof(smtp.Host));
        if (smtp.Port <= 0)
            throw new ArgumentOutOfRangeException(nameof(smtp.Port), "SMTP port must be greater than 0.");
        if (string.IsNullOrWhiteSpace(smtp.FromEmail))
            throw new ArgumentException("FromEmail is required.", nameof(smtp.FromEmail));
        
        // In development, allow empty credentials for local testing
        var isDevelopment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development";
        if (!isDevelopment)
        {
            if (string.IsNullOrWhiteSpace(smtp.Username))
                throw new ArgumentException("SMTP username is required.", nameof(smtp.Username));
            if (string.IsNullOrWhiteSpace(smtp.Password))
                throw new ArgumentException("SMTP password is required.", nameof(smtp.Password));
        }
    }

    private static string GenerateEmailHtml(string action, string link)
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

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            _smtpClient?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}