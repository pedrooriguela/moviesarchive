using MoviesArchive.Domain.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MoviesArchive.Infra.Settings;
using MailKit;
using Microsoft.Extensions.Configuration;

namespace MoviesArchive.Infra.Services;

public class EmailService : IEmailService
{
    private readonly SmtpSettings _settings;
    public EmailService(IConfiguration configuration) 
    {
        _settings = configuration.GetSection("SmtpSettings").Get<SmtpSettings>()!;
    }
    
    public async Task SendAsync(string toEmail, string subject, string body)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromEmail));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(_settings.Username, _settings.Password);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);
    }    
}