using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace NammaOoru.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, string otp, string recipientName);
        Task SendWelcomeEmailAsync(string email, string recipientName);
        Task SendNotificationEmailAsync(string email, string recipientName, string subject, string message);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        // ---------------------- COMMON SMTP METHOD ---------------------- //
        private async Task SendEmailAsync(MimeMessage message)
        {
            string smtpServer = _config["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(_config["EmailSettings:SmtpPort"]);
            string senderEmail = _config["EmailSettings:SenderEmail"];
            string senderPassword = _config["EmailSettings:SenderPassword"];

            using var client = new SmtpClient();

            try
            {
                client.Timeout = 60000;
                client.AuthenticationMechanisms.Remove("XOAUTH2");

                _logger.LogInformation("SMTP → Connecting to {Server}:{Port}", smtpServer, smtpPort);

                // Brevo requires STARTTLS
                await client.ConnectAsync(
                    smtpServer,
                    smtpPort,
                    SecureSocketOptions.StartTls
                );

                _logger.LogInformation("SMTP → Connected successfully.");

                await client.AuthenticateAsync(senderEmail, senderPassword);

                _logger.LogInformation("SMTP → Authenticated as {Email}", senderEmail);

                await client.SendAsync(message);

                _logger.LogInformation("SMTP → Email sent successfully.");
            }
            finally
            {
                await client.DisconnectAsync(true);
            }
        }

        // ---------------------- OTP EMAIL ---------------------- //
        public async Task SendOtpEmailAsync(string email, string otp, string recipientName)
        {
            const int maxRetries = 3;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var msg = new MimeMessage();
                    msg.From.Add(new MailboxAddress("NammaOoru - OTP Verification", _config["EmailSettings:SenderEmail"]));
                    msg.To.Add(new MailboxAddress(recipientName, email));
                    msg.Subject = "🔐 Your OTP Verification Code - NammaOoru";

                    msg.Body = new BodyBuilder
                    {
                        HtmlBody = $@"
                        <html>
                            <body style='font-family: Arial; background-color:#f8f8f8; padding:20px;'>
                                <div style='max-width:600px; margin:auto; background:white; padding:20px; border-radius:8px;'>
                                    <h2 style='background:#667eea; color:white; padding:15px; border-radius:8px; text-align:center;'>
                                        NammaOoru Email Verification
                                    </h2>

                                    <p>Hello {recipientName},</p>
                                    <p>Your OTP code is:</p>

                                    <div style='font-size:38px; font-weight:bold; text-align:center; color:#667eea; letter-spacing:8px;'>
                                        {otp}
                                    </div>

                                    <p style='text-align:center;'>⏱️ Valid for 10 minutes.</p>

                                    <div style='margin-top:20px; padding:12px; background:#fff4c2; border-radius:5px;'>
                                        <b>⚠ DO NOT SHARE</b> — No one from NammaOoru will ask for your OTP.
                                    </div>

                                    <p style='text-align:center; margin-top:30px; color:#888;'>© 2025 NammaOoru</p>
                                </div>
                            </body>
                        </html>"
                    }.ToMessageBody();

                    _logger.LogInformation("OTP → Attempt {Attempt}/{Max}", attempt, maxRetries);
                    await SendEmailAsync(msg);
                    return;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "OTP → Attempt {Attempt} failed.", attempt);

                    if (attempt == maxRetries)
                    {
                        _logger.LogError(ex, "OTP → ALL RETRIES FAILED.");
                        throw;
                    }

                    await Task.Delay(2000 * attempt);
                }
            }
        }

        // ---------------------- WELCOME EMAIL ---------------------- //
        public async Task SendWelcomeEmailAsync(string email, string recipientName)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("NammaOoru", _config["EmailSettings:SenderEmail"]));
            msg.To.Add(new MailboxAddress(recipientName, email));
            msg.Subject = "🎉 Welcome to NammaOoru!";

            msg.Body = new BodyBuilder
            {
                HtmlBody = $@"
                <html>
                    <body style='font-family:Arial; background:#f3f3f3; padding:20px;'>
                        <div style='max-width:600px; margin:auto; background:white; padding:20px; border-radius:8px;'>
                            <h2 style='background:#4CAF50; padding:15px; color:white; border-radius:8px; text-align:center;'>
                                Welcome to NammaOoru!
                            </h2>

                            <p>Hello {recipientName},</p>
                            <p>Your email has been verified successfully.</p>
                            <p>Start exploring everything NammaOoru offers!</p>

                            <p style='text-align:center; margin-top:20px; color:#666;'>© 2025 NammaOoru</p>
                        </div>
                    </body>
                </html>"
            }.ToMessageBody();

            await SendEmailAsync(msg);
        }

        // ---------------------- GENERIC NOTIFICATION EMAIL ---------------------- //
        public async Task SendNotificationEmailAsync(string email, string recipientName, string subject, string message)
        {
            var msg = new MimeMessage();
            msg.From.Add(new MailboxAddress("NammaOoru Notifications", _config["EmailSettings:SenderEmail"]));
            msg.To.Add(new MailboxAddress(recipientName, email));
            msg.Subject = subject;

            msg.Body = new BodyBuilder
            {
                HtmlBody = $@"
                <html>
                    <body style='font-family:Arial; background:#f3f3f3; padding:20px;'>
                        <div style='max-width:600px; margin:auto; background:white; padding:20px; border-radius:8px;'>
                            <h2 style='background:#4CAF50; padding:15px; color:white; border-radius:8px; text-align:center;'>
                                Notification
                            </h2>

                            <p>Hello {recipientName},</p>
                            <p>{message}</p>

                            <p style='text-align:center; margin-top:20px; color:#666;'>© 2025 NammaOoru</p>
                        </div>
                    </body>
                </html>"
            }.ToMessageBody();

            await SendEmailAsync(msg);
        }
    }
}
