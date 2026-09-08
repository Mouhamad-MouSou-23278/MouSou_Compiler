using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;

namespace OnlineCompiler.Infrastructure.Email;

public class EmailService : IEmailService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task SendVerificationEmailAsync(string toEmail, string token)
    {
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var verificationUrl = $"{frontendUrl}/verify-email?email={System.Uri.EscapeDataString(toEmail)}&token={token}";

        _logger.LogInformation(
            "================ EMAIL NOTIFICATION ================\n" +
            "To: {ToEmail}\n" +
            "Subject: Verify your Online Compiler V2 Account\n" +
            "Verification Link: {Link}\n" +
            "Token: {Token}\n" +
            "====================================================",
            toEmail, verificationUrl, token);

        return Task.CompletedTask;
    }

    public Task SendPasswordResetEmailAsync(string toEmail, string token)
    {
        var frontendUrl = _configuration["App:FrontendUrl"] ?? "http://localhost:3000";
        var resetUrl = $"{frontendUrl}/reset-password?email={System.Uri.EscapeDataString(toEmail)}&token={token}";

        _logger.LogInformation(
            "================ PASSWORD RESET ================\n" +
            "To: {ToEmail}\n" +
            "Subject: Reset your Online Compiler V2 Password\n" +
            "Reset Link: {Link}\n" +
            "Token: {Token}\n" +
            "================================================",
            toEmail, resetUrl, token);

        return Task.CompletedTask;
    }
}
