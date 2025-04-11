using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Net.Sockets;

namespace AccreditationSystem.Pages.Services
{

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> emailSettings, ILogger<EmailService> logger)
        {
            _emailSettings = emailSettings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                _logger.LogInformation($"Creating email message to {email} with subject: {subject}");

                var emailMessage = new MimeMessage();
                emailMessage.From.Add(new MailboxAddress(_emailSettings.SenderName, _emailSettings.SenderEmail));
                emailMessage.To.Add(MailboxAddress.Parse(email));
                emailMessage.Subject = subject;

                var bodyBuilder = new BodyBuilder { HtmlBody = message };
                emailMessage.Body = bodyBuilder.ToMessageBody();

                _logger.LogInformation($"Attempting to connect to SMTP server: {_emailSettings.SmtpServer}:{_emailSettings.SmtpPort}");

                using var client = new SmtpClient();

                // Add these troubleshooting enhancements:

                // 1. Bypass SSL certificate validation issues
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                // 2. Increase timeout
                client.Timeout = 60000; // 60 seconds timeout

                // 3. Set more detailed client options
                // Remove the line causing the error
                // var options = new SocketOptions(client);

                // 4. Try with Auto instead of StartTls for more flexible protocol negotiation
                try
                {
                    _logger.LogInformation("Connecting with SecureSocketOptions.Auto...");
                    await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.Auto);
                }
                catch (Exception connectEx)
                {
                    _logger.LogWarning($"First connection attempt failed: {connectEx.Message}. Trying with explicit SSL setting.");

                    // 5. If Auto fails, try with explicit SSL (port 465) or StartTls (port 587)
                    if (_emailSettings.SmtpPort == 465)
                    {
                        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.SslOnConnect);
                    }
                    else
                    {
                        await client.ConnectAsync(_emailSettings.SmtpServer, _emailSettings.SmtpPort, SecureSocketOptions.StartTls);
                    }
                }

                _logger.LogInformation("Successfully connected to SMTP server");
                _logger.LogInformation($"Authenticating with username: {_emailSettings.SmtpUsername}");

                // 6. Authenticate with more detailed logging
                await client.AuthenticateAsync(_emailSettings.SmtpUsername, _emailSettings.SmtpPassword);
                _logger.LogInformation("Authentication successful");

                // 7. Send with more detailed logging
                _logger.LogInformation("Sending email message...");
                await client.SendAsync(emailMessage);
                _logger.LogInformation("Email sent successfully");

                await client.DisconnectAsync(true);
                _logger.LogInformation($"Email sent successfully to {email}");
            }
            catch (SocketException socketEx)
            {
                // 8. Handle specific socket exceptions with more details
                _logger.LogError($"Socket error sending email: {socketEx.Message}, Error code: {socketEx.ErrorCode}, SocketErrorCode: {socketEx.SocketErrorCode}");

                if (socketEx.SocketErrorCode == SocketError.TimedOut)
                {
                    _logger.LogError("Connection timed out. This could be due to firewall or network issues.");
                }
                else if (socketEx.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    _logger.LogError("Connection refused. The SMTP server may be blocking connections or is not available.");
                }

                _logger.LogError($"Complete socket exception details: {socketEx}");
                throw;
            }
            catch (Exception ex)
            {
                // 9. Capture full exception details for deeper analysis
                _logger.LogError($"Error sending email: {ex.Message}");
                _logger.LogError($"Exception type: {ex.GetType().Name}");
                _logger.LogError($"Stack trace: {ex.StackTrace}");

                if (ex.InnerException != null)
                {
                    _logger.LogError($"Inner exception: {ex.InnerException.Message}");
                    _logger.LogError($"Inner exception type: {ex.InnerException.GetType().Name}");
                }

                _logger.LogError($"Complete exception details: {ex}");
                throw;
            }
        }
    }
}
