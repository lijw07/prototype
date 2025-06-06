using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
using Prototype.POCO;

namespace Prototype.Services;

public class EmailNotificationService : IEmailNotificationService
{
    private readonly SmtpClient _smtpClient;
    private readonly string _fromEmail;

    public EmailNotificationService(IOptions<SmtpSettingsPoco> smtpOptions)
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
        var body = $@"
            <html>
              <body>
                <p>Hello,</p>
                <p>Please click the button below to verify your account:</p>
                <a href='http://localhost:8080/verify?email={recipientEmail}&code={verificationCode}' 
                   style='display: inline-block; padding: 10px 20px; font-size: 16px;
                          color: #fff; background-color: #007BFF; text-decoration: none; border-radius: 5px;'>
                  Verify Email
                </a>
                <p>If the button doesn't work, you can also copy and paste the link into your browser:</p>
                <p>http://localhost:8080/verify?email={recipientEmail}&code={verificationCode}</p>
              </body>
            </html>";
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

    public async Task SendPasswordResetEmail(string recipientEmail, string resetLink)
    {
        var subject = "Reset Your Password";
        var body = $@"
            <html>
              <body>
                <p>Hello,</p>
                <p>We received a request to reset your password. Click the button below to proceed:</p>
                <a href='{resetLink}' 
                   style='display: inline-block; padding: 10px 20px; font-size: 16px;
                          color: #fff; background-color: #dc3545; text-decoration: none; border-radius: 5px;'>
                  Reset Password
                </a>
                <p>If you didn’t request this, you can safely ignore this email.</p>
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
}
