using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.AI;
using OnlineCompiler.Application.DTOs.Executions;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Services;

public class AIFacadeService : IAIFacadeService
{
    private readonly IApplicationDbContext _context;
    private readonly IAIService _aiService;
    private readonly IExecutionService _executionService;

    public AIFacadeService(
        IApplicationDbContext context,
        IAIService aiService,
        IExecutionService executionService)
    {
        _context = context;
        _aiService = aiService;
        _executionService = executionService;
    }

    public async Task<AIAnalyzeResponse> AnalyzeAsync(Guid userId, AIAnalyzeRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId && p.UserId == userId && !p.IsArchived);

        if (project == null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        var filesDict = project.Files.ToDictionary(f => f.Path, f => f.Content);

        var (analysis, suggestedCode, diffPatch) = await _aiService.AnalyzeAsync(
            project.Language,
            filesDict,
            request.SpecificFilePath,
            request.Type,
            request.CustomPrompt
        );

        // Record AI job linked to the latest execution if available
        var latestExecution = await _context.Executions
            .Where(e => e.ProjectId == project.Id)
            .OrderByDescending(e => e.StartedAt)
            .FirstOrDefaultAsync();

        var aiJob = new AIJob
        {
            Id = Guid.NewGuid(),
            ExecutionId = latestExecution?.Id ?? Guid.Empty,
            Type = request.Type,
            Request = request.CustomPrompt ?? request.Type.ToString(),
            Suggestion = analysis,
            DiffPatch = diffPatch,
            Applied = false,
            CreatedAt = DateTime.UtcNow
        };

        if (latestExecution != null)
        {
            _context.AIJobs.Add(aiJob);
            await _context.SaveChangesAsync();
        }

        return new AIAnalyzeResponse(
            aiJob.Id,
            request.Type,
            analysis,
            suggestedCode,
            diffPatch
        );
    }

    public async Task<AIAutoFixResponse> AutoFixAsync(Guid userId, AIAutoFixRequest request)
    {
        var execution = await _context.Executions
            .Include(e => e.Project)
            .ThenInclude(p => p!.Files)
            .FirstOrDefaultAsync(e => e.Id == request.ExecutionId && e.UserId == userId);

        if (execution == null || execution.Project == null)
        {
            throw new KeyNotFoundException("Execution not found.");
        }

        var project = execution.Project;
        var filesDict = project.Files.ToDictionary(f => f.Path, f => f.Content);
        var entryPoint = project.Files.FirstOrDefault(f => f.IsEntryPoint)?.Path ?? project.Files.First().Path;

        var errorOutput = $"{execution.Error}\n{execution.Output}";

        var fixResult = await _aiService.GenerateFixAsync(
            project.Language,
            filesDict,
            entryPoint,
            errorOutput
        );

        // Find file to patch
        var targetFile = project.Files.FirstOrDefault(f => f.Path == fixResult.TargetFilePath)
                         ?? project.Files.FirstOrDefault(f => f.IsEntryPoint)
                         ?? project.Files.FirstOrDefault();

        bool applied = false;
        Guid? newExecutionId = null;

        if (targetFile != null && !string.IsNullOrWhiteSpace(fixResult.FixedContent))
        {
            targetFile.Content = fixResult.FixedContent;
            targetFile.UpdatedAt = DateTime.UtcNow;
            applied = true;

            var aiJob = new AIJob
            {
                Id = Guid.NewGuid(),
                ExecutionId = execution.Id,
                Type = AIJobType.BugFix,
                Request = "Auto-fix from execution error",
                Suggestion = fixResult.Diagnosis,
                DiffPatch = fixResult.DiffPatch,
                Applied = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.AIJobs.Add(aiJob);
            await _context.SaveChangesAsync();

            // Re-run execution automatically with the fix applied!
            var rerunResponse = await _executionService.EnqueueExecutionAsync(userId, new RunExecutionRequest(
                project.Id,
                execution.StdinInput,
                AutoFixOnError: false
            ));

            newExecutionId = rerunResponse.Id;
        }

        return new AIAutoFixResponse(
            Guid.NewGuid(),
            fixResult.Diagnosis,
            targetFile?.Path,
            fixResult.FixedContent,
            fixResult.DiffPatch,
            applied,
            newExecutionId
        );
    }
}
