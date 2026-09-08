using System;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.Auth;

namespace OnlineCompiler.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task RevokeRefreshTokenAsync(string refreshToken);
    Task<UserProfileResponse> GetProfileAsync(Guid userId);
    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task ConfirmEmailAsync(ConfirmEmailRequest request);
    Task InitiatePasswordResetAsync(ResetPasswordRequest request);
    Task ResetPasswordAsync(ResetPasswordConfirmRequest request);
}
