using System.Net;
using System.Net.Mail;

namespace Prototype.Utility;

public class EmailNotification
{
    public static async Task SendVerificationEmail(string toEmail, string code)
    {
        var client = new SmtpClient("sandbox.smtp.mailtrap.io", 2525)
        {
            Credentials = new NetworkCredential("98fdd0b970f7e7", "ae639c51eb3547"),
            EnableSsl = true
        };

        var fromEmail = "noreply@example.com";
        var subject = "Email Verification Code";
        var body = $"Your verification code is: {code}";

        var mail = new MailMessage(fromEmail, toEmail, subject, body);

        await client.SendMailAsync(mail);
    }
}