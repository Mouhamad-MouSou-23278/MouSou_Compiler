using System;

namespace OnlineCompiler.Domain.Entities;

public enum AIJobType
{
    Explain,
    BugFix,
    Refactor,
    TestGeneration,
    SecurityAnalysis,
    PerformanceAnalysis,
    Documentation
}

public class AIJob
{
    public Guid Id { get; set; }
    public Guid ExecutionId { get; set; }
    public AIJobType Type { get; set; }
    public string Request { get; set; } = null!; // User request description
    public string? Suggestion { get; set; }
    public string? DiffPatch { get; set; }
    public bool Applied { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Execution? Execution { get; set; }
}
