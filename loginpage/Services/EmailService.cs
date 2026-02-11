using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace loginpage.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string verificationUrl);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string verificationUrl)
        {
            var mode = (_config["Email:Mode"] ?? "Console").Trim();

            if (string.Equals(mode, "Console", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"[VERIFY LINK] To: {toEmail}");
                Console.WriteLine(verificationUrl);
                return;
            }

            var smtp = _config.GetSection("Smtp");
            var host = (smtp["Host"] ?? "").Trim();
            var portStr = (smtp["Port"] ?? "587").Trim();
            var from = (smtp["From"] ?? "").Trim();
            var user = (smtp["User"] ?? "").Trim();
            var pass = (smtp["Pass"] ?? "").Trim();

            if (string.IsNullOrWhiteSpace(host))
            {
                Console.WriteLine($"[EMAIL ERROR] Smtp:Host empty. Link: {verificationUrl}");
                return;
            }

            if (!int.TryParse(portStr, out var port)) port = 587;

            var message = new MimeMessage();
            message.From.Add(MailboxAddress.Parse(string.IsNullOrWhiteSpace(from) ? user : from));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = "Verify your account";
            message.Body = new TextPart("plain")
            {
                Text = $"Please verify your account by clicking: {verificationUrl}"
            };

            using var client = new SmtpClient();

            try
            {
                Console.WriteLine($"[EMAIL] Connecting to {host}:{port} (StartTls) ...");
                await client.ConnectAsync(host, port, SecureSocketOptions.StartTls);

                if (!string.IsNullOrWhiteSpace(user))
                {
                    if (string.IsNullOrWhiteSpace(pass))
                    {
                        Console.WriteLine("[EMAIL ERROR] Smtp:Pass empty (App Password kerak).");
                        await client.DisconnectAsync(true);
                        return;
                    }

                    Console.WriteLine($"[EMAIL] Authenticating as {user} ...");
                    await client.AuthenticateAsync(user, pass);
                }

                Console.WriteLine($"[EMAIL] Sending to {toEmail} ...");
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
                Console.WriteLine("[EMAIL] Sent OK ✅");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] {ex.GetType().Name}: {ex.Message}");
            }
        }
    }
}
