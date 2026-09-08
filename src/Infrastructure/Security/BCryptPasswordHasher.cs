using OnlineCompiler.Application.Common.Interfaces;
using BCrypt.Net;

namespace OnlineCompiler.Infrastructure.Security;

public class BCryptPasswordHasher : IPasswordHasher
{
    public string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password)) password = Guid.NewGuid().ToString("N");
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, 12);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(passwordHash))
            return false;

        return BCrypt.Net.BCrypt.EnhancedVerify(password, passwordHash);
    }
}
