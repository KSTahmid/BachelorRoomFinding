using System.Net;
using System.Net.Mail;

namespace BachelorRoomFinding.Services
{
    public class EmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                var smtpServer = _config["EmailSettings:SmtpServer"] ?? _config["EmailSettings:SmtpHost"] ?? "smtp.gmail.com";
                var portString = _config["EmailSettings:Port"] ?? _config["EmailSettings:SmtpPort"] ?? "587";
                int port = int.Parse(portString);
                var senderEmail = _config["EmailSettings:SenderEmail"];
                var senderPassword = _config["EmailSettings:SenderPassword"] ?? _config["EmailSettings:AppPassword"];

                if (string.IsNullOrEmpty(senderEmail) || string.IsNullOrEmpty(senderPassword))
                {
                    _logger.LogWarning("EmailSettings are missing in appsettings.json. Falling back to Fake Email logging.");
                    _logger.LogInformation("📧 [FAKE EMAIL] To: {To} | Subject: {Subject}\n{Body}", toEmail, subject, htmlBody);
                    return;
                }

                using var client = new SmtpClient(smtpServer, port)
                {
                    Credentials = new NetworkCredential(senderEmail, senderPassword),
                    EnableSsl = true
                };

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, "MessBasha"),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email successfully sent to {ToEmail}", toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {ToEmail}", toEmail);
            }
        }

        public Task SendVerificationAsync(string toEmail, string userName, string token)
        {
            var link = $"https://localhost:5108/Account/VerifyEmail?token={token}";
            return SendAsync(toEmail, "Verify your MessBasha email",
                $"Hi {userName},<br><br>Welcome to MessBasha! Please verify your email by clicking the link below:<br><br><a href='{link}'>{link}</a>");
        }

        public Task SendPasswordResetAsync(string toEmail, string userName, string token)
        {
            var link = $"https://localhost:5108/Account/ResetPassword?token={token}";
            return SendAsync(toEmail, "Reset your MessBasha password",
                $"Hi {userName},<br><br>You requested a password reset for your MessBasha account. Click the link below to reset it:<br><br><a href='{link}'>{link}</a>");
        }
    }
}
