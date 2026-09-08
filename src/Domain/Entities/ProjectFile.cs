using System;
using System.Collections.Generic;

namespace OnlineCompiler.Domain.Entities;

public class ProjectFile
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Path { get; set; } = null!; // e.g., "src/main.py"
    public string Content { get; set; } = string.Empty;
    public bool IsEntryPoint { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public Project? Project { get; set; }
}
