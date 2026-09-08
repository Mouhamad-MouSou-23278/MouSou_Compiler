using System.Threading.Tasks;

namespace OnlineCompiler.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendVerificationEmailAsync(string toEmail, string token);
    Task SendPasswordResetEmailAsync(string toEmail, string token);
}
