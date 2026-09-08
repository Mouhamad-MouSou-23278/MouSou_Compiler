using System;
using System.Collections.Generic;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.DTOs.Executions;

public record RunExecutionRequest(
    Guid ProjectId,
    string? StdinInput,
    bool AutoFixOnError = false
);

public record ExecutionResponse(
    Guid Id,
    Guid ProjectId,
    string Language,
    ExecutionStatus Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int? ExitCode,
    string Trigger
);

public record ExecutionDetailResponse(
    Guid Id,
    Guid ProjectId,
    string Language,
    ExecutionStatus Status,
    DateTime StartedAt,
    DateTime? FinishedAt,
    int? ExitCode,
    string? Output,
    string? Error,
    string? StdinInput,
    string? MetricsJson,
    string Trigger
);

public record ExecutionLogMessage(
    Guid ExecutionId,
    string Stream, // "stdout" | "stderr" | "system"
    string Text,
    DateTime Timestamp
);

public record ExecutionJobPayload(
    Guid ExecutionId,
    Guid ProjectId,
    Guid UserId,
    string Language,
    string EntryPoint,
    Dictionary<string, string> Files,
    string? StdinInput,
    bool AutoFixOnError
);

public record SendInputRequest(
    string Input
);
