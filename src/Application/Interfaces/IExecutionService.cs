using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.Executions;

namespace OnlineCompiler.Application.Interfaces;

public interface IExecutionService
{
    Task<ExecutionResponse> EnqueueExecutionAsync(Guid userId, RunExecutionRequest request);
    Task<ExecutionDetailResponse> GetExecutionByIdAsync(Guid executionId, Guid userId);
    Task<List<ExecutionResponse>> GetProjectExecutionsAsync(Guid projectId, Guid userId, int limit = 20);
    Task SendInputAsync(Guid executionId, Guid userId, string input);
}
