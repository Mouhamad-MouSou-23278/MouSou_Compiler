using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Files;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Services;

public class FileService : IFileService
{
    private readonly IApplicationDbContext _context;

    public FileService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<FileResponse>> GetFilesAsync(Guid projectId, Guid userId)
    {
        await VerifyProjectOwnershipAsync(projectId, userId);

        return await _context.ProjectFiles
            .AsNoTracking()
            .Where(f => f.ProjectId == projectId)
            .OrderByDescending(f => f.IsEntryPoint)
            .ThenBy(f => f.Path)
            .Select(f => new FileResponse(f.Id, f.ProjectId, f.Path, f.Content, f.IsEntryPoint, f.CreatedAt, f.UpdatedAt))
            .ToListAsync();
    }

    public async Task<FileResponse> GetFileByIdAsync(Guid fileId, Guid userId)
    {
        var file = await _context.ProjectFiles
            .Include(f => f.Project)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.Project != null && f.Project.UserId == userId);

        if (file == null)
        {
            throw new KeyNotFoundException("File not found.");
        }

        return new FileResponse(file.Id, file.ProjectId, file.Path, file.Content, file.IsEntryPoint, file.CreatedAt, file.UpdatedAt);
    }

    public async Task<FileResponse> CreateFileAsync(Guid projectId, Guid userId, CreateFileRequest request)
    {
        await VerifyProjectOwnershipAsync(projectId, userId);

        var normalizedPath = request.Path.Trim().Replace('\\', '/');

        var exists = await _context.ProjectFiles.AnyAsync(f => f.ProjectId == projectId && f.Path == normalizedPath);
        if (exists)
        {
            throw new InvalidOperationException($"File '{normalizedPath}' already exists in this project.");
        }

        var now = DateTime.UtcNow;
        var file = new ProjectFile
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Path = normalizedPath,
            Content = request.Content ?? string.Empty,
            IsEntryPoint = request.IsEntryPoint,
            CreatedAt = now,
            UpdatedAt = now
        };

        if (request.IsEntryPoint)
        {
            // Clear other entry points
            var currentEntryPoints = await _context.ProjectFiles
                .Where(f => f.ProjectId == projectId && f.IsEntryPoint)
                .ToListAsync();
            foreach (var ep in currentEntryPoints)
            {
                ep.IsEntryPoint = false;
            }
        }

        _context.ProjectFiles.Add(file);
        await _context.SaveChangesAsync();

        return new FileResponse(file.Id, file.ProjectId, file.Path, file.Content, file.IsEntryPoint, file.CreatedAt, file.UpdatedAt);
    }

    public async Task<FileResponse> UpdateFileAsync(Guid fileId, Guid userId, UpdateFileRequest request)
    {
        var file = await _context.ProjectFiles
            .Include(f => f.Project)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.Project != null && f.Project.UserId == userId);

        if (file == null)
        {
            throw new KeyNotFoundException("File not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Path))
        {
            file.Path = request.Path.Trim().Replace('\\', '/');
        }

        if (request.Content != null)
        {
            file.Content = request.Content;
        }

        if (request.IsEntryPoint.HasValue && request.IsEntryPoint.Value && !file.IsEntryPoint)
        {
            var otherEntryPoints = await _context.ProjectFiles
                .Where(f => f.ProjectId == file.ProjectId && f.Id != file.Id && f.IsEntryPoint)
                .ToListAsync();
            foreach (var ep in otherEntryPoints)
            {
                ep.IsEntryPoint = false;
            }
            file.IsEntryPoint = true;
        }

        file.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return new FileResponse(file.Id, file.ProjectId, file.Path, file.Content, file.IsEntryPoint, file.CreatedAt, file.UpdatedAt);
    }

    public async Task DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await _context.ProjectFiles
            .Include(f => f.Project)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.Project != null && f.Project.UserId == userId);

        if (file == null)
        {
            throw new KeyNotFoundException("File not found.");
        }

        _context.ProjectFiles.Remove(file);
        await _context.SaveChangesAsync();
    }

    public async Task SetEntryPointAsync(Guid fileId, Guid userId)
    {
        var file = await _context.ProjectFiles
            .Include(f => f.Project)
            .FirstOrDefaultAsync(f => f.Id == fileId && f.Project != null && f.Project.UserId == userId);

        if (file == null)
        {
            throw new KeyNotFoundException("File not found.");
        }

        var allProjectFiles = await _context.ProjectFiles
            .Where(f => f.ProjectId == file.ProjectId)
            .ToListAsync();

        foreach (var pf in allProjectFiles)
        {
            pf.IsEntryPoint = pf.Id == fileId;
        }

        await _context.SaveChangesAsync();
    }

    public async Task SaveAllFilesAsync(Guid projectId, Guid userId, Dictionary<string, string> files)
    {
        await VerifyProjectOwnershipAsync(projectId, userId);

        var existingFiles = await _context.ProjectFiles
            .Where(f => f.ProjectId == projectId)
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var kvp in files)
        {
            var normalizedPath = kvp.Key.Trim().Replace('\\', '/');
            var existing = existingFiles.FirstOrDefault(f => f.Path == normalizedPath);
            if (existing != null)
            {
                existing.Content = kvp.Value;
                existing.UpdatedAt = now;
            }
            else
            {
                _context.ProjectFiles.Add(new ProjectFile
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    Path = normalizedPath,
                    Content = kvp.Value,
                    IsEntryPoint = existingFiles.Count == 0,
                    CreatedAt = now,
                    UpdatedAt = now
                });
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task VerifyProjectOwnershipAsync(Guid projectId, Guid userId)
    {
        var exists = await _context.Projects.AnyAsync(p => p.Id == projectId && p.UserId == userId && !p.IsArchived);
        if (!exists)
        {
            throw new KeyNotFoundException("Project not found or unauthorized.");
        }
    }
}
