using System;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.AI;

namespace OnlineCompiler.Application.Interfaces;

public interface IAIFacadeService
{
    Task<AIAnalyzeResponse> AnalyzeAsync(Guid userId, AIAnalyzeRequest request);
    Task<AIAutoFixResponse> AutoFixAsync(Guid userId, AIAutoFixRequest request);
}
