using MimeKit;
using SmtpClient = MailKit.Net.Smtp.SmtpClient;
using DbProxy.Server.Models;

namespace DbProxy.Server.Services;

public class EmailService(EmailConfiguration configuration)
{
    public async Task SendConnectionNotificationAsync(string token)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(name: configuration.FromName, address: configuration.FromAddress));
        message.To.Add(new MailboxAddress(name: configuration.ToName, address: configuration.ToAddress));
        message.Subject = configuration.Subject;

        message.Body = new TextPart("plain")
        {
            Text = configuration.TextTemplate.Replace("{{TOKEN}}", token)
        };

        using var smtpClient = new SmtpClient();
        await smtpClient.ConnectAsync(host: configuration.SmtpHost, port: configuration.SmtpPort, useSsl: true);
        await smtpClient.AuthenticateAsync(userName: configuration.SmtpUsername, password: configuration.SmtpPassword);
        await smtpClient.SendAsync(message);
        await smtpClient.DisconnectAsync(quit: true);
    }
}