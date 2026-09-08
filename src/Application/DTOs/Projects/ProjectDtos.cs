using System;
using System.Collections.Generic;
using OnlineCompiler.Application.DTOs.Files;

namespace OnlineCompiler.Application.DTOs.Projects;

public record CreateProjectRequest(
    string Name,
    string? Description,
    string Language
);

public record UpdateProjectRequest(
    string Name,
    string? Description,
    string? Language
);

public record ProjectResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string Language,
    bool IsArchived,
    int FileCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record ProjectDetailResponse(
    Guid Id,
    Guid UserId,
    string Name,
    string? Description,
    string Language,
    bool IsArchived,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<FileResponse> Files
);
