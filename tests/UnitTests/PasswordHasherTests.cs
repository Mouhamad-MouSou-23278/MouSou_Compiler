using FluentAssertions;
using OnlineCompiler.Infrastructure.Security;
using Xunit;

namespace OnlineCompiler.UnitTests;

public class PasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void HashPassword_ShouldReturnValidBcryptHash()
    {
        // Act
        var hash = _hasher.HashPassword("P@ssw0rd123!");

        // Assert
        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().StartWith("$2");
    }

    [Fact]
    public void VerifyPassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var password = "SecureEnterprisePassword2026!";
        var hash = _hasher.HashPassword(password);

        // Act
        var result = _hasher.VerifyPassword(password, hash);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void VerifyPassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var hash = _hasher.HashPassword("CorrectPassword");

        // Act
        var result = _hasher.VerifyPassword("WrongPassword", hash);

        // Assert
        result.Should().BeFalse();
    }
}
