using System.Net.Mail;

namespace Core.Security.Sender;

public interface IEmailSender
{
    void Send(string to, string subject, string body);
}

public sealed class SmtpEmailSender : IEmailSender
{
    public void Send(string to, string subject, string body)
    {
        SendEmail.Send(to, subject, body);
    }
}

// Kept as a compatibility facade for existing call sites outside the account controller.
public static class SendEmail
{
    public static void Send(string to, string subject, string body)
    {
        using var mail = new MailMessage();
        using var smtpServer = new SmtpClient("smtp.gmail.com");

        mail.From = new MailAddress("myasancode@gmail.com", "آسان کد");
        mail.To.Add(to);
        mail.Subject = subject;
        mail.Body = body;
        mail.IsBodyHtml = true;

        smtpServer.UseDefaultCredentials = false;
        smtpServer.Port = 587;
        smtpServer.Credentials = new System.Net.NetworkCredential(
            "myasancode@gmail.com",
            "_12_12AhmaD@smtp");
        smtpServer.EnableSsl = true;
        smtpServer.Send(mail);
    }
}
