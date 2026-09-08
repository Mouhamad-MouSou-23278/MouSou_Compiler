using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Auth;
using OnlineCompiler.Application.Services;
using OnlineCompiler.Infrastructure.Persistence;
using OnlineCompiler.Infrastructure.Security;
using Xunit;

namespace OnlineCompiler.UnitTests;

public class AuthServiceTests
{
    private OnlineCompilerDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<OnlineCompilerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new OnlineCompilerDbContext(options);
    }

    [Fact]
    public async Task RegisterAsync_WithNewEmail_ShouldReturnTokensAndUser()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var passwordHasher = new BCryptPasswordHasher();
        var jwtGeneratorMock = new Mock<IJwtTokenGenerator>();
        jwtGeneratorMock.Setup(j => j.GenerateAccessToken(It.IsAny<Domain.Entities.User>())).Returns("fake-access-token");
        jwtGeneratorMock.Setup(j => j.GenerateRefreshToken()).Returns("fake-refresh-token");

        var emailServiceMock = new Mock<IEmailService>();
        var logger = NullLogger<AuthService>.Instance;

        var authService = new AuthService(context, passwordHasher, jwtGeneratorMock.Object, emailServiceMock.Object, logger);

        var request = new RegisterRequest("dev@mousou.com", "SecureP@ss123", "John", "Doe");

        // Act
        var result = await authService.RegisterAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Email.Should().Be("dev@mousou.com");
        result.AccessToken.Should().Be("fake-access-token");
        result.RefreshToken.Should().Be("fake-refresh-token");

        var savedUser = await context.Users.FirstOrDefaultAsync(u => u.Email == "dev@mousou.com");
        savedUser.Should().NotBeNull();
        passwordHasher.VerifyPassword("SecureP@ss123", savedUser!.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task RegisterAsync_WithDuplicateEmail_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var context = CreateInMemoryDbContext();
        var passwordHasher = new BCryptPasswordHasher();
        var jwtGeneratorMock = new Mock<IJwtTokenGenerator>();
        jwtGeneratorMock.Setup(j => j.GenerateAccessToken(It.IsAny<Domain.Entities.User>())).Returns("fake-access-token");
        jwtGeneratorMock.Setup(j => j.GenerateRefreshToken()).Returns("fake-refresh-token");
        var emailServiceMock = new Mock<IEmailService>();
        var logger = NullLogger<AuthService>.Instance;

        var authService = new AuthService(context, passwordHasher, jwtGeneratorMock.Object, emailServiceMock.Object, logger);

        var request = new RegisterRequest("duplicate@mousou.com", "Password123", "A", "B");
        await authService.RegisterAsync(request);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(() => authService.RegisterAsync(request));
    }
}
