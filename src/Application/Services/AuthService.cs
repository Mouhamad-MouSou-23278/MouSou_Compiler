using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Auth;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Services;

public class AuthService : IAuthService
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IEmailService _emailService;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IEmailService emailService,
        ILogger<AuthService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var existingUser = await _context.Users.AnyAsync(u => u.Email == normalizedEmail);
        if (existingUser)
        {
            throw new InvalidOperationException("User with this email already exists.");
        }

        var now = DateTime.UtcNow;
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            FirstName = request.FirstName?.Trim(),
            LastName = request.LastName?.Trim(),
            EmailConfirmed = true, // default to true for seamless onboarding in dev/sandbox
            Role = UserRole.User,
            CreatedAt = now,
            UpdatedAt = now
        };

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        user.RefreshTokenHash = _passwordHasher.HashPassword(refreshToken);
        user.RefreshTokenExpiry = now.AddDays(14);

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Send verification email in background
        _ = _emailService.SendVerificationEmailAsync(user.Email, Guid.NewGuid().ToString());

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var normalizedEmail = request.Email.Trim().ToLowerInvariant();

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == normalizedEmail);
        if (user == null || !_passwordHasher.VerifyPassword(request.Password, user.PasswordHash))
        {
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        var refreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        user.RefreshTokenHash = _passwordHasher.HashPassword(refreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(14);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAccessException("Invalid refresh token.");
        }

        var users = await _context.Users
            .Where(u => u.RefreshTokenHash != null && u.RefreshTokenExpiry > DateTime.UtcNow)
            .ToListAsync();

        var user = users.FirstOrDefault(u => _passwordHasher.VerifyPassword(refreshToken, u.RefreshTokenHash!));
        if (user == null)
        {
            throw new UnauthorizedAccessException("Expired or invalid refresh token.");
        }

        // Rotate refresh token
        var newRefreshToken = _jwtTokenGenerator.GenerateRefreshToken();
        user.RefreshTokenHash = _passwordHasher.HashPassword(newRefreshToken);
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(14);
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var accessToken = _jwtTokenGenerator.GenerateAccessToken(user);

        return new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = newRefreshToken,
            UserId = user.Id,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role
        };
    }

    public async Task RevokeRefreshTokenAsync(string refreshToken)
    {
        if (string.IsNullOrWhiteSpace(refreshToken)) return;

        var users = await _context.Users
            .Where(u => u.RefreshTokenHash != null)
            .ToListAsync();

        var user = users.FirstOrDefault(u => _passwordHasher.VerifyPassword(refreshToken, u.RefreshTokenHash!));
        if (user != null)
        {
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiry = null;
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    public async Task<UserProfileResponse> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.EmailConfirmed,
            user.CreatedAt
        );
    }

    public async Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.FirstName = request.FirstName?.Trim();
        user.LastName = request.LastName?.Trim();
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.EmailConfirmed,
            user.CreatedAt
        );
    }

    public async Task ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        if (!_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            throw new InvalidOperationException("Current password is incorrect.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.RefreshTokenHash = null; // Invalidate all active sessions
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }

    public async Task ConfirmEmailAsync(ConfirmEmailRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.EmailConfirmed = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task InitiatePasswordResetAsync(ResetPasswordRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());
        if (user != null)
        {
            var resetToken = Guid.NewGuid().ToString("N");
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetToken);
            _logger.LogInformation("Initiated password reset for user {Email}", user.Email);
        }
    }

    public async Task ResetPasswordAsync(ResetPasswordConfirmRequest request)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email.Trim().ToLowerInvariant());
        if (user == null)
        {
            throw new KeyNotFoundException("User not found.");
        }

        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.RefreshTokenHash = null;
        user.RefreshTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
    }
}
