using System;
using System.Collections.Generic;

namespace OnlineCompiler.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string Language { get; set; } = "python";
    public bool IsArchived { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public User? Owner { get; set; }
    public ICollection<ProjectFile> Files { get; set; } = new List<ProjectFile>();
    public ICollection<Execution> Executions { get; set; } = new List<Execution>();
}
