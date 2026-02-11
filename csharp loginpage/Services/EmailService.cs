using System.Threading.Tasks;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace loginpage.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string verificationUrl);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        public EmailService(IConfiguration config) => _config = config;

        public async Task SendVerificationEmailAsync(string toEmail, string verificationUrl)
        {
            var smtp = _config.GetSection("Smtp");
            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(smtp["From"]));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Verify your account";

            message.Body = new TextPart("plain")
            {
                Text = $"Please verify: {verificationUrl}"
            };

            // note: send asynchronously; failures should be logged but don't block registration
            using var client = new SmtpClient();
            await client.ConnectAsync(smtp["Host"], int.Parse(smtp["Port"] ?? "587"), MailKit.Security.SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(smtp["User"], smtp["Pass"]);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}