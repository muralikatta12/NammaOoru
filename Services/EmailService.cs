using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NammaOoru.Services
{
    public interface IEmailService
    {
        Task SendOtpEmailAsync(string email, string otp, string recipientName);
        Task SendWelcomeEmailAsync(string email, string recipientName);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string email, string otp, string recipientName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Moodly", _configuration["EmailSettings:SenderEmail"]));
                message.To.Add(new MailboxAddress(recipientName, email));
                message.Subject = "Your OTP Verification Code";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                            <head>
                                <style>
                                    body {{ font-family: Arial, sans-serif; }}
                                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                                    .content {{ background-color: #f9f9f9; padding: 20px; margin-top: 20px; border-radius: 5px; }}
                                    .otp-code {{ font-size: 32px; font-weight: bold; color: #4CAF50; text-align: center; margin: 20px 0; letter-spacing: 5px; }}
                                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                                </style>
                            </head>
                            <body>
                                <div class='container'>
                                    <div class='header'>
                                        <h1>Moodly - Email Verification</h1>
                                    </div>
                                    <div class='content'>
                                        <p>Hi {recipientName},</p>
                                        <p>Your OTP verification code is:</p>
                                        <div class='otp-code'>{otp}</div>
                                        <p>This code will expire in 10 minutes.</p>
                                        <p>Do not share this code with anyone.</p>
                                    </div>
                                    <div class='footer'>
                                        <p>© 2025 Moodly. All rights reserved.</p>
                                    </div>
                                </div>
                            </body>
                        </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                    var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail is not configured");
                    var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? throw new InvalidOperationException("SenderPassword is not configured");
                    
                    _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{SmtpPort} with email {Email}", smtpServer, smtpPort, senderEmail);
                    
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    _logger.LogInformation("Connected to SMTP server successfully");
                    
                    // Remove XOAUTH2 to force basic auth
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    _logger.LogInformation("Authenticated successfully with {Email}", senderEmail);
                    
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("OTP email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending OTP email to {Email}: {Message}", email, ex.Message);
                throw;
            }
        }

        public async Task SendWelcomeEmailAsync(string email, string recipientName)
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Moodly", _configuration["EmailSettings:SenderEmail"]));
                message.To.Add(new MailboxAddress(recipientName, email));
                message.Subject = "Welcome to Moodly!";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                        <html>
                            <head>
                                <style>
                                    body {{ font-family: Arial, sans-serif; }}
                                    .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                    .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                                    .content {{ background-color: #f9f9f9; padding: 20px; margin-top: 20px; border-radius: 5px; }}
                                    .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                                </style>
                            </head>
                            <body>
                                <div class='container'>
                                    <div class='header'>
                                        <h1>Welcome to Moodly!</h1>
                                    </div>
                                    <div class='content'>
                                        <p>Hi {recipientName},</p>
                                        <p>Welcome to Moodly - Your Personal Mood Tracker!</p>
                                        <p>Your email has been verified and your account is now active.</p>
                                        <p>Start tracking your moods and getting insights into your emotional well-being.</p>
                                    </div>
                                    <div class='footer'>
                                        <p>© 2025 Moodly. All rights reserved.</p>
                                    </div>
                                </div>
                            </body>
                        </html>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                using (var client = new SmtpClient())
                {
                    var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp.gmail.com";
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                    var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail is not configured");
                    var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? throw new InvalidOperationException("SenderPassword is not configured");
                    
                    _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{SmtpPort} with email {Email}", smtpServer, smtpPort, senderEmail);
                    
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    _logger.LogInformation("Connected to SMTP server successfully");
                    
                    // Remove XOAUTH2 to force basic auth
                    client.AuthenticationMechanisms.Remove("XOAUTH2");
                    
                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    _logger.LogInformation("Authenticated successfully with {Email}", senderEmail);
                    
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                _logger.LogInformation("Welcome email sent successfully to {Email}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending welcome email to {Email}: {Message}", email, ex.Message);
                throw;
            }
        }
    }
}
