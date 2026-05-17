namespace BachelorRoomFinding.Services
{
    /// <summary>
    /// Fake email service that logs emails to the console.
    /// Replace with real SMTP (e.g. SendGrid, MailKit) in production.
    /// </summary>
    public class FakeEmailService
    {
        private readonly ILogger<FakeEmailService> _logger;

        public FakeEmailService(ILogger<FakeEmailService> logger) => _logger = logger;

        public Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            _logger.LogInformation(
                "📧 [FAKE EMAIL] To: {To} | Subject: {Subject}\n{Body}",
                toEmail, subject, htmlBody);

            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"\n========== FAKE EMAIL ==========");
            Console.WriteLine($"To:      {toEmail}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine($"Body:    {htmlBody}");
            Console.WriteLine($"================================\n");
            Console.ResetColor();

            return Task.CompletedTask;
        }

        public Task SendVerificationAsync(string toEmail, string userName, string token)
        {
            var link = $"https://localhost/Account/VerifyEmail?token={token}";
            return SendAsync(toEmail, "Verify your BRF email",
                $"Hi {userName}, click to verify: <a href='{link}'>{link}</a>");
        }

        public Task SendPasswordResetAsync(string toEmail, string userName, string token)
        {
            var link = $"https://localhost/Account/ResetPassword?token={token}";
            return SendAsync(toEmail, "Reset your BRF password",
                $"Hi {userName}, click to reset password: <a href='{link}'>{link}</a>");
        }
    }
}
