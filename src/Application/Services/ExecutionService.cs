using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Executions;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Services;

public class ExecutionService : IExecutionService
{
    private readonly IApplicationDbContext _context;
    private readonly IExecutionQueue _queue;

    public ExecutionService(IApplicationDbContext context, IExecutionQueue queue)
    {
        _context = context;
        _queue = queue;
    }

    public async Task<ExecutionResponse> EnqueueExecutionAsync(Guid userId, RunExecutionRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.UserId == userId && !p.IsArchived);

        if (project == null)
        {
            throw new KeyNotFoundException("Project not found or unauthorized.");
        }

        if (project.Files.Count == 0)
        {
            throw new InvalidOperationException("Project contains no files to execute.");
        }

        var entryFile = project.Files.FirstOrDefault(f => f.IsEntryPoint) ?? project.Files.First();

        var execution = new Execution
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            Language = project.Language,
            Status = ExecutionStatus.Queued,
            StartedAt = DateTime.UtcNow,
            StdinInput = request.StdinInput,
            Trigger = request.AutoFixOnError ? "AutoFix" : "Manual"
        };

        _context.Executions.Add(execution);
        await _context.SaveChangesAsync();

        var filesDict = project.Files.ToDictionary(f => f.Path, f => f.Content);

        var payload = new ExecutionJobPayload(
            execution.Id,
            project.Id,
            userId,
            project.Language,
            entryFile.Path,
            filesDict,
            request.StdinInput,
            request.AutoFixOnError
        );

        await _queue.EnqueueAsync(payload);

        return new ExecutionResponse(
            execution.Id,
            execution.ProjectId,
            execution.Language,
            execution.Status,
            execution.StartedAt,
            execution.FinishedAt,
            execution.ExitCode,
            execution.Trigger
        );
    }

    public async Task<ExecutionDetailResponse> GetExecutionByIdAsync(Guid executionId, Guid userId)
    {
        var execution = await _context.Executions
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == executionId && e.UserId == userId);

        if (execution == null)
        {
            throw new KeyNotFoundException("Execution not found or unauthorized.");
        }

        return new ExecutionDetailResponse(
            execution.Id,
            execution.ProjectId,
            execution.Language,
            execution.Status,
            execution.StartedAt,
            execution.FinishedAt,
            execution.ExitCode,
            execution.Output,
            execution.Error,
            execution.StdinInput,
            execution.MetricsJson,
            execution.Trigger
        );
    }

    public async Task<List<ExecutionResponse>> GetProjectExecutionsAsync(Guid projectId, Guid userId, int limit = 20)
    {
        var projectExists = await _context.Projects.AnyAsync(p => p.Id == projectId && p.UserId == userId);
        if (!projectExists)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        return await _context.Executions
            .AsNoTracking()
            .Where(e => e.ProjectId == projectId)
            .OrderByDescending(e => e.StartedAt)
            .Take(limit)
            .Select(e => new ExecutionResponse(
                e.Id,
                e.ProjectId,
                e.Language,
                e.Status,
                e.StartedAt,
                e.FinishedAt,
                e.ExitCode,
                e.Trigger
            ))
            .ToListAsync();
    }

    public async Task SendInputAsync(Guid executionId, Guid userId, string input)
    {
        var exists = await _context.Executions.AnyAsync(e => e.Id == executionId && e.UserId == userId);
        if (!exists)
        {
            throw new KeyNotFoundException("Execution not found.");
        }

        await _queue.PublishInputAsync(executionId, input);
    }
}
