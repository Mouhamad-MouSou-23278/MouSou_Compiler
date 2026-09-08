using System;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.DTOs.AI;

public record AIAnalyzeRequest(
    Guid ProjectId,
    AIJobType Type,
    string? SpecificFilePath,
    string? CustomPrompt
);

public record AIAnalyzeResponse(
    Guid JobId,
    AIJobType Type,
    string Analysis,
    string? SuggestedCode,
    string? DiffPatch
);

public record AIAutoFixRequest(
    Guid ExecutionId
);

public record AIAutoFixResponse(
    Guid JobId,
    string ErrorDiagnosis,
    string? TargetFilePath,
    string? FixedCode,
    string? DiffPatch,
    bool SuccessfullyPatched,
    Guid? NewExecutionId
);
