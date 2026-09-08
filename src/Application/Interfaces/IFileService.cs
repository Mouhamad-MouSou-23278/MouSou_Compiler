using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using OnlineCompiler.Application.DTOs.Files;

namespace OnlineCompiler.Application.Interfaces;

public interface IFileService
{
    Task<List<FileResponse>> GetFilesAsync(Guid projectId, Guid userId);
    Task<FileResponse> GetFileByIdAsync(Guid fileId, Guid userId);
    Task<FileResponse> CreateFileAsync(Guid projectId, Guid userId, CreateFileRequest request);
    Task<FileResponse> UpdateFileAsync(Guid fileId, Guid userId, UpdateFileRequest request);
    Task DeleteFileAsync(Guid fileId, Guid userId);
    Task SetEntryPointAsync(Guid fileId, Guid userId);
    Task SaveAllFilesAsync(Guid projectId, Guid userId, Dictionary<string, string> files);
}
