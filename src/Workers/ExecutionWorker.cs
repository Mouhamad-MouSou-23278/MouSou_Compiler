using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.AI;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Workers;

public class ExecutionWorker : BackgroundService
{
    private readonly IExecutionQueue _queue;
    private readonly IDockerSandboxService _sandboxService;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ExecutionWorker> _logger;

    public ExecutionWorker(
        IExecutionQueue queue,
        IDockerSandboxService sandboxService,
        IServiceScopeFactory scopeFactory,
        ILogger<ExecutionWorker> logger)
    {
        _queue = queue;
        _sandboxService = sandboxService;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ExecutionWorker started. Listening for sandboxed execution jobs...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                if (job == null)
                {
                    await Task.Delay(500, stoppingToken);
                    continue;
                }

                _logger.LogInformation("Processing job {ExecutionId} for language {Language}...", job.ExecutionId, job.Language);
                await ProcessExecutionJobAsync(job, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ExecutionWorker loop.");
                await Task.Delay(2000, stoppingToken);
            }
        }

        _logger.LogInformation("ExecutionWorker is stopping.");
    }

    private async Task ProcessExecutionJobAsync(Application.DTOs.Executions.ExecutionJobPayload job, CancellationToken stoppingToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

        var execution = await context.Executions.FirstOrDefaultAsync(e => e.Id == job.ExecutionId, stoppingToken);
        if (execution == null)
        {
            _logger.LogWarning("Execution record {Id} not found in database.", job.ExecutionId);
            return;
        }

        execution.Status = ExecutionStatus.Running;
        await context.SaveChangesAsync(stoppingToken);

        var result = await _sandboxService.ExecuteAsync(
            job,
            async (stream, chunk) =>
            {
                _logger.LogDebug("[{ExecutionId}][{Stream}] {Chunk}", job.ExecutionId, stream, chunk.TrimEnd());
                await Task.CompletedTask;
            },
            stoppingToken
        );

        execution.ExitCode = result.ExitCode;
        execution.Output = result.Output;
        execution.Error = result.Error;
        execution.FinishedAt = DateTime.UtcNow;

        if (result.TimedOut)
        {
            execution.Status = ExecutionStatus.Timeout;
        }
        else if (result.MemoryExceeded)
        {
            execution.Status = ExecutionStatus.MemoryExceeded;
        }
        else if (result.ExitCode == 0)
        {
            execution.Status = ExecutionStatus.Completed;
        }
        else
        {
            execution.Status = ExecutionStatus.Failed;
        }

        execution.MetricsJson = JsonSerializer.Serialize(new
        {
            executionTimeMs = result.ExecutionTime.TotalMilliseconds,
            timedOut = result.TimedOut,
            memoryExceeded = result.MemoryExceeded
        });

        await context.SaveChangesAsync(stoppingToken);
        _logger.LogInformation("Finished job {ExecutionId} with status {Status} in {Ms}ms", job.ExecutionId, execution.Status, result.ExecutionTime.TotalMilliseconds);

        // Auto-fix loop if requested and failed
        if (job.AutoFixOnError && execution.Status == ExecutionStatus.Failed)
        {
            try
            {
                _logger.LogInformation("Job {ExecutionId} failed. Triggering automated AI auto-fix loop...", job.ExecutionId);
                var aiFacade = scope.ServiceProvider.GetRequiredService<IAIFacadeService>();
                var fixResponse = await aiFacade.AutoFixAsync(job.UserId, new AIAutoFixRequest(job.ExecutionId));
                _logger.LogInformation("AI AutoFix applied: {Success}, New Execution: {NewId}", fixResponse.SuccessfullyPatched, fixResponse.NewExecutionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to execute auto-fix for {ExecutionId}", job.ExecutionId);
            }
        }
    }
}
