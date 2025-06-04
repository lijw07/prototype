using System.Net.Mail;

namespace Prototype.Utility;

public class EmailNotification
{
    public static async Task SendVerificationEmail(string toEmail, string code)
    {
        var fromEmail = "no-reply@yourdomain.com";
        var smtp = new SmtpClient("your.smtp.server.com")
        {
            Port = 587,
            Credentials = new System.Net.NetworkCredential("your-username", "your-password"),
            EnableSsl = true,
        };

        var mail = new MailMessage(fromEmail, toEmail)
        {
            Subject = "Verify your email",
            Body = $"Your verification code is: {code}"
        };

        await smtp.SendMailAsync(mail);
    }
}