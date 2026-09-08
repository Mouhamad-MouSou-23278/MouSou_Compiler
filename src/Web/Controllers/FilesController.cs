using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCompiler.Application.DTOs.Files;
using OnlineCompiler.Application.Interfaces;

namespace OnlineCompiler.Web.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;

    public FilesController(IFileService fileService)
    {
        _fileService = fileService;
    }

    [HttpGet("projects/{projectId:guid}/files")]
    public async Task<ActionResult<List<FileResponse>>> GetFiles(Guid projectId)
    {
        var userId = GetCurrentUserId();
        var files = await _fileService.GetFilesAsync(projectId, userId);
        return Ok(files);
    }

    [HttpGet("files/{id:guid}")]
    public async Task<ActionResult<FileResponse>> GetFile(Guid id)
    {
        var userId = GetCurrentUserId();
        var file = await _fileService.GetFileByIdAsync(id, userId);
        return Ok(file);
    }

    [HttpPost("projects/{projectId:guid}/files")]
    public async Task<ActionResult<FileResponse>> CreateFile(Guid projectId, [FromBody] CreateFileRequest request)
    {
        var userId = GetCurrentUserId();
        var file = await _fileService.CreateFileAsync(projectId, userId, request);
        return CreatedAtAction(nameof(GetFile), new { id = file.Id }, file);
    }

    [HttpPut("files/{id:guid}")]
    public async Task<ActionResult<FileResponse>> UpdateFile(Guid id, [FromBody] UpdateFileRequest request)
    {
        var userId = GetCurrentUserId();
        var file = await _fileService.UpdateFileAsync(id, userId, request);
        return Ok(file);
    }

    [HttpDelete("files/{id:guid}")]
    public async Task<IActionResult> DeleteFile(Guid id)
    {
        var userId = GetCurrentUserId();
        await _fileService.DeleteFileAsync(id, userId);
        return NoContent();
    }

    [HttpPost("files/{id:guid}/entry-point")]
    public async Task<IActionResult> SetEntryPoint(Guid id)
    {
        var userId = GetCurrentUserId();
        await _fileService.SetEntryPointAsync(id, userId);
        return Ok(new { message = "Entry point updated." });
    }

    [HttpPost("projects/{projectId:guid}/files/batch")]
    public async Task<IActionResult> SaveAllFiles(Guid projectId, [FromBody] Dictionary<string, string> files)
    {
        var userId = GetCurrentUserId();
        await _fileService.SaveAllFilesAsync(projectId, userId, files);
        return Ok(new { message = "Files saved successfully." });
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        if (Guid.TryParse(claim, out var id))
        {
            return id;
        }
        throw new UnauthorizedAccessException("Unauthorized user context.");
    }
}
