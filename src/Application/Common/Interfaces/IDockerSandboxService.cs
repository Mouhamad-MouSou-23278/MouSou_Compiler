using System;
using System.Threading;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.Executions;

namespace OnlineCompiler.Application.Common.Interfaces;

public record SandboxResult(
    int ExitCode,
    string Output,
    string Error,
    TimeSpan ExecutionTime,
    bool TimedOut,
    bool MemoryExceeded
);

public interface IDockerSandboxService
{
    Task<SandboxResult> ExecuteAsync(
        ExecutionJobPayload job,
        Func<string, string, Task> onLogReceived, // stream: "stdout"|"stderr"|"system", text
        CancellationToken cancellationToken = default
    );
}
