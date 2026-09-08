using System;
using System.Collections.Generic;

namespace OnlineCompiler.Domain.Entities;

public enum ExecutionStatus
{
    Queued,
    Running,
    Completed,
    Failed,
    Timeout,
    MemoryExceeded
}

public class Execution
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid UserId { get; set; }
    public string Language { get; set; } = "python";
    public ExecutionStatus Status { get; set; } = ExecutionStatus.Queued;
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? FinishedAt { get; set; }
    public int? ExitCode { get; set; }
    public string? Output { get; set; }
    public string? Error { get; set; }
    public string? StdinInput { get; set; }
    public string? MetricsJson { get; set; } // JSON for CPU, memory etc.
    public string Trigger { get; set; } = "Manual"; // Manual, AIFix, etc.

    public Project? Project { get; set; }
    public User? User { get; set; }
    public ICollection<AIJob> AIJobs { get; set; } = new List<AIJob>();
}
