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
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOtpEmailAsync(string email, string otp, string recipientName)
        {
            const int maxRetries = 3;
            const int initialDelayMs = 2000; // Start with 2 second delay

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress("NammaOoru - OTP Verification", _configuration["EmailSettings:SenderEmail"]));
                    message.To.Add(new MailboxAddress(recipientName, email));
                    message.Subject = "🔐 Your OTP Verification Code - NammaOoru";
                    
                    // Add headers for better deliverability
                    message.Headers.Add("X-Priority", "3");

                    var bodyBuilder = new BodyBuilder
                    {
                        HtmlBody = $@"
                            <html>
                                <head>
                                    <style>
                                        body {{ font-family: Arial, sans-serif; }}
                                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                                        .header {{ background-color: #667eea; color: white; padding: 20px; text-align: center; border-radius: 5px; }}
                                        .content {{ background-color: #f9f9f9; padding: 20px; margin-top: 20px; border-radius: 5px; }}
                                        .otp-code {{ font-size: 36px; font-weight: bold; color: #667eea; text-align: center; margin: 20px 0; letter-spacing: 6px; font-family: monospace; }}
                                        .footer {{ text-align: center; margin-top: 20px; color: #666; font-size: 12px; }}
                                        .warning {{ background-color: #fff3cd; padding: 10px; border-radius: 4px; margin-top: 10px; color: #666; }}
                                    </style>
                                </head>
                                <body>
                                    <div class='container'>
                                        <div class='header'>
                                            <h1>🔐 NammaOoru</h1>
                                            <p style='margin: 0;'>Email Verification</p>
                                        </div>
                                        <div class='content'>
                                            <p>Hi {recipientName},</p>
                                            <p><strong>Your OTP verification code is:</strong></p>
                                            <div class='otp-code'>{otp}</div>
                                            <p><strong>⏱️ Valid for 10 minutes only</strong></p>
                                            <p>Do not share this code with anyone.</p>
                                            <div class='warning'>
                                                <strong>⚠️ Important:</strong> Never share this OTP with anyone. NammaOoru staff will never ask for your OTP.
                                            </div>
                                        </div>
                                        <div class='footer'>
                                            <p>© 2025 NammaOoru. All rights reserved.</p>
                                        </div>
                                    </div>
                                </body>
                            </html>"
                    };

                    message.Body = bodyBuilder.ToMessageBody();

                    using (var client = new SmtpClient())
                    {
                        // Set timeout to 60 seconds for slow connections
                        client.Timeout = 60000;
                        var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp-relay.brevo.com";
                        var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587"); // Try port 587 (SSL) by default
                        var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail is not configured");
                        var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? throw new InvalidOperationException("SenderPassword is not configured");
                        
                        _logger.LogInformation(" [OTP] Attempt {Attempt}/{MaxRetries} | Sending to {Email}", attempt, maxRetries, email);
                        _logger.LogInformation(" [OTP] SMTP: {Server}:{Port} | From: {From}", smtpServer, smtpPort, senderEmail);
                        
                        // Try to determine secure socket options based on port
                        var secureSocketOptions = smtpPort == 587 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                        await client.ConnectAsync(smtpServer, smtpPort, secureSocketOptions);
                        _logger.LogInformation("✅ [OTP] Connected to SMTP");
                        
                        client.AuthenticationMechanisms.Remove("XOAUTH2");
                        
                        try
                        {
                            await client.AuthenticateAsync(senderEmail, senderPassword);
                            _logger.LogInformation("✅ [OTP] Authenticated as {Email}", senderEmail);
                        }
                        catch (Exception authEx)
                        {
                            _logger.LogError(authEx, "❌ [OTP] AUTHENTICATION FAILED!");
                            _logger.LogError("❌ [OTP] Check your SMTP credentials in appsettings.json");
                            _logger.LogError("❌ [OTP] SenderEmail must be a verified sender in Brevo");
                            throw;
                        }
                        
                        try
                        {
                            await client.SendAsync(message);
                            _logger.LogInformation("✅ [OTP] Email sent to SMTP queue");
                        }
                        catch (Exception sendEx)
                        {
                            _logger.LogError(sendEx, "❌ [OTP] SEND FAILED!");
                            _logger.LogError("❌ [OTP] Error: {Message}", sendEx.Message);
                            throw;
                        }
                        
                        await client.DisconnectAsync(true);
                        _logger.LogInformation("✅ [OTP] Disconnected from SMTP");
                    }

                    _logger.LogInformation("✅ [OTP] Email delivered to {Email}", email);
                    return; // Success, exit method
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "⚠️ [OTP] Attempt {Attempt} failed: {Message}", attempt, ex.Message);
                    
                    if (attempt == maxRetries)
                    {
                        // Last attempt failed
                        _logger.LogError(ex, "❌ [OTP] All {MaxRetries} attempts failed to send OTP to {Email}", maxRetries, email);
                        throw;
                    }
                    
                    // Wait before retrying (exponential backoff)
                    int delayMs = initialDelayMs * (int)Math.Pow(2, attempt - 1);
                    _logger.LogInformation("⏳ [OTP] Waiting {DelayMs}ms before retry {NextAttempt}...", delayMs, attempt + 1);
                    await Task.Delay(delayMs);
                }
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
                    // avoid indefinite blocking if SMTP is unreachable
                    client.Timeout = 60000; // 60s
                    var smtpServer = _configuration["EmailSettings:SmtpServer"] ?? "smtp-relay.brevo.com";
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"] ?? "587");
                    var senderEmail = _configuration["EmailSettings:SenderEmail"] ?? throw new InvalidOperationException("SenderEmail is not configured");
                    var senderPassword = _configuration["EmailSettings:SenderPassword"] ?? throw new InvalidOperationException("SenderPassword is not configured");
                    
                    _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{SmtpPort} with email {Email}", smtpServer, smtpPort, senderEmail);
                    
                    var secureSocketOptions = smtpPort == 587 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                    await client.ConnectAsync(smtpServer, smtpPort, secureSocketOptions);
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

        public async Task SendNotificationEmailAsync(
            string email,
            string recipientName,
            string subject,
            string message)
        {
            try
            {
                // create a MIME email message
                var mimeMessage = new MimeMessage();
                mimeMessage.From.Add(new MailboxAddress("NammaOoru", _configuration["EmailSettings:SenderEmail"]));
                mimeMessage.To.Add(new MailboxAddress(recipientName, email));
                mimeMessage.Subject = subject;


                //email body

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody =  $@"
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
                                        <h1>NammaOoru Notification</h1>
                                    </div>
                                    <div class='content'>
                                        <p>Hi {recipientName},</p>
                                        <p>{message}</p>
                                    </div>
                                    <div class='footer'>
                                        <p>© 2024 NammaOoru. All rights reserved.</p>
                                    </div>
                                </div>
                            </body>
                        </html>"
                };

                mimeMessage.Body = bodyBuilder.ToMessageBody();


                //connect to smtp server and send email

                using (var client = new SmtpClient())
                {
                        // avoid indefinite blocking if SMTP is unreachable
                        client.Timeout = 60000; // 60s
                    var smtpServer = _configuration["EmailSettings:SmtpServer"]?? "smtp-relay.brevo.com";
                    var smtpPort = int.Parse(_configuration["EmailSettings:SmtpPort"]?? "587");
                    var senderEmail =  _configuration["EmailSettings:SenderEmail"];
                    var senderPassword =  _configuration["EmailSettings:SenderPassword"];


                    _logger.LogInformation("Connecting to SMTP server {SmtpServer}:{SmtpPort} with email {Email}", smtpServer, smtpPort, senderEmail);

                    var secureSocketOptions = smtpPort == 587 ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTls;
                    await client.ConnectAsync(smtpServer, smtpPort, secureSocketOptions);
                    _logger.LogInformation("Connected to SMTP server successfully");

                    // Remove XOAUTH2 to force basic auth

                    client.AuthenticationMechanisms.Remove("XOAUTH2");

                    await client.AuthenticateAsync(senderEmail, senderPassword);
                    _logger.LogInformation("Authenticated successfully with {Email}", senderEmail);


                    await client.SendAsync(mimeMessage);
                    await client.DisconnectAsync(true);

                    _logger.LogInformation("Notification email sent successfully to {Email}", email);

                }
            }

            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification email to {Email}: {Message}", email, ex.Message);
                throw;
            }
        }
    }
}
