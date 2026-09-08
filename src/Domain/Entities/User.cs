using System;
using System.Collections.Generic;

namespace OnlineCompiler.Domain.Entities;

public enum UserRole
{
    User,
    Admin
}

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool EmailConfirmed { get; set; }
    public UserRole Role { get; set; } = UserRole.User;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? RefreshTokenHash { get; set; }
    public DateTime? RefreshTokenExpiry { get; set; }

    public ICollection<Project> Projects { get; set; } = new List<Project>();
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
}
