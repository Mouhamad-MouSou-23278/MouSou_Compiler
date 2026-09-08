using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.Common.Interfaces;
using OnlineCompiler.Application.DTOs.Files;
using OnlineCompiler.Application.DTOs.Projects;
using OnlineCompiler.Application.Interfaces;
using OnlineCompiler.Domain.Entities;

namespace OnlineCompiler.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IApplicationDbContext _context;

    public ProjectService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProjectResponse>> GetProjectsAsync(Guid userId)
    {
        return await _context.Projects
            .AsNoTracking()
            .Where(p => p.UserId == userId && !p.IsArchived)
            .OrderByDescending(p => p.UpdatedAt)
            .Select(p => new ProjectResponse(
                p.Id,
                p.UserId,
                p.Name,
                p.Description,
                p.Language,
                p.IsArchived,
                p.Files.Count,
                p.CreatedAt,
                p.UpdatedAt
            ))
            .ToListAsync();
    }

    public async Task<ProjectDetailResponse> GetProjectByIdAsync(Guid projectId, Guid userId)
    {
        var project = await _context.Projects
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId && !p.IsArchived);

        if (project == null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        var files = project.Files
            .OrderByDescending(f => f.IsEntryPoint)
            .ThenBy(f => f.Path)
            .Select(f => new FileResponse(f.Id, f.ProjectId, f.Path, f.Content, f.IsEntryPoint, f.CreatedAt, f.UpdatedAt))
            .ToList();

        return new ProjectDetailResponse(
            project.Id,
            project.UserId,
            project.Name,
            project.Description,
            project.Language,
            project.IsArchived,
            project.CreatedAt,
            project.UpdatedAt,
            files
        );
    }

    public async Task<ProjectDetailResponse> CreateProjectAsync(Guid userId, CreateProjectRequest request)
    {
        var now = DateTime.UtcNow;
        var lang = string.IsNullOrWhiteSpace(request.Language) ? "python" : request.Language.Trim().ToLowerInvariant();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            Language = lang,
            IsArchived = false,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create default starter file based on language
        var (entryPath, initialContent) = GetDefaultStarterFile(lang);
        var starterFile = new ProjectFile
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Path = entryPath,
            Content = initialContent,
            IsEntryPoint = true,
            CreatedAt = now,
            UpdatedAt = now
        };

        project.Files.Add(starterFile);
        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return new ProjectDetailResponse(
            project.Id,
            project.UserId,
            project.Name,
            project.Description,
            project.Language,
            project.IsArchived,
            project.CreatedAt,
            project.UpdatedAt,
            new List<FileResponse>
            {
                new(starterFile.Id, starterFile.ProjectId, starterFile.Path, starterFile.Content, starterFile.IsEntryPoint, starterFile.CreatedAt, starterFile.UpdatedAt)
            }
        );
    }

    public async Task<ProjectDetailResponse> UpdateProjectAsync(Guid projectId, Guid userId, UpdateProjectRequest request)
    {
        var project = await _context.Projects
            .Include(p => p.Files)
            .FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId && !p.IsArchived);

        if (project == null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        if (!string.IsNullOrWhiteSpace(request.Language))
        {
            project.Language = request.Language.Trim().ToLowerInvariant();
        }
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        var files = project.Files
            .OrderByDescending(f => f.IsEntryPoint)
            .ThenBy(f => f.Path)
            .Select(f => new FileResponse(f.Id, f.ProjectId, f.Path, f.Content, f.IsEntryPoint, f.CreatedAt, f.UpdatedAt))
            .ToList();

        return new ProjectDetailResponse(
            project.Id,
            project.UserId,
            project.Name,
            project.Description,
            project.Language,
            project.IsArchived,
            project.CreatedAt,
            project.UpdatedAt,
            files
        );
    }

    public async Task DeleteProjectAsync(Guid projectId, Guid userId)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.UserId == userId);
        if (project == null)
        {
            throw new KeyNotFoundException("Project not found.");
        }

        project.IsArchived = true;
        project.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    private static (string Path, string Content) GetDefaultStarterFile(string language) => language switch
    {
        "javascript" or "node" => ("index.js", "// Online Compiler V2 - Node.js\nconsole.log('Hello from Node.js Sandbox!');\n"),
        "csharp" or "c#" => ("Program.cs", "// Online Compiler V2 - C# 8.0\nusing System;\n\nConsole.WriteLine(\"Hello from C# Sandbox!\");\n"),
        "go" or "golang" => ("main.go", "// Online Compiler V2 - Go\npackage main\n\nimport \"fmt\"\n\nfunc main() {\n    fmt.Println(\"Hello from Go Sandbox!\")\n}\n"),
        "cpp" or "c++" => ("main.cpp", "// Online Compiler V2 - C++\n#include <iostream>\n\nint main() {\n    std::cout << \"Hello from C++ Sandbox!\" << std::endl;\n    return 0;\n}\n"),
        "rust" => ("main.rs", "// Online Compiler V2 - Rust\nfn main() {\n    println!(\"Hello from Rust Sandbox!\");\n}\n"),
        _ => ("main.py", "# Online Compiler V2 - Python 3\ndef main():\n    print(\"Hello from Python Sandbox!\")\n\nif __name__ == \"__main__\":\n    main()\n")
    };
}
