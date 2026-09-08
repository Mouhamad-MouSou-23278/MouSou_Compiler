using System.Threading;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.Executions;

namespace OnlineCompiler.Application.Common.Interfaces;

public interface IExecutionQueue
{
    Task EnqueueAsync(ExecutionJobPayload payload, CancellationToken cancellationToken = default);
    Task<ExecutionJobPayload?> DequeueAsync(CancellationToken cancellationToken = default);
    Task PublishInputAsync(System.Guid executionId, string input, CancellationToken cancellationToken = default);
}
