using System;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.DTOs.Auth;

public record RegisterRequest(
    string Email,
    string Password,
    string? FirstName,
    string? LastName
);

public record LoginRequest(
    string Email,
    string Password
);

public class AuthResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string? RefreshToken { get; set; }
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public UserRole Role { get; set; }
}

public record UserProfileResponse(
    Guid Id,
    string Email,
    string? FirstName,
    string? LastName,
    UserRole Role,
    bool EmailConfirmed,
    DateTime CreatedAt
);

public record UpdateProfileRequest(
    string? FirstName,
    string? LastName
);

public record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword
);

public record ResetPasswordRequest(
    string Email
);

public record ResetPasswordConfirmRequest(
    string Email,
    string Token,
    string NewPassword
);

public record ConfirmEmailRequest(
    string Email,
    string Token
);
