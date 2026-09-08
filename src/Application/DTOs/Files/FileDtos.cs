using System;

namespace OnlineCompiler.Application.DTOs.Files;

public record CreateFileRequest(
    string Path,
    string Content,
    bool IsEntryPoint
);

public record UpdateFileRequest(
    string? Path,
    string? Content,
    bool? IsEntryPoint
);

public record FileResponse(
    Guid Id,
    Guid ProjectId,
    string Path,
    string Content,
    bool IsEntryPoint,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
