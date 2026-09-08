using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.Projects;

namespace OnlineCompiler.Application.Interfaces;

public interface IProjectService
{
    Task<List<ProjectResponse>> GetProjectsAsync(Guid userId);
    Task<ProjectDetailResponse> GetProjectByIdAsync(Guid projectId, Guid userId);
    Task<ProjectDetailResponse> CreateProjectAsync(Guid userId, CreateProjectRequest request);
    Task<ProjectDetailResponse> UpdateProjectAsync(Guid projectId, Guid userId, UpdateProjectRequest request);
    Task DeleteProjectAsync(Guid projectId, Guid userId);
}
