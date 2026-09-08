using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Common.Interfaces;

public record AIAutoFixResult(
    string Diagnosis,
    string TargetFilePath,
    string FixedContent,
    string DiffPatch
);

public interface IAIService
{
    Task<string> PromptAsync(string systemPrompt, string userPrompt, CancellationToken cancellationToken = default);

    Task<(string Analysis, string? SuggestedCode, string? DiffPatch)> AnalyzeAsync(
        string language,
        Dictionary<string, string> files,
        string? targetFile,
        AIJobType type,
        string? customPrompt,
        CancellationToken cancellationToken = default
    );

    Task<AIAutoFixResult> GenerateFixAsync(
        string language,
        Dictionary<string, string> files,
        string entryPoint,
        string errorOutput,
        CancellationToken cancellationToken = default
    );
}
