using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCompiler.Application.DTOs.Projects;
using OnlineCompiler.Application.Interfaces;

namespace OnlineCompiler.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;

    public ProjectsController(IProjectService projectService)
    {
        _projectService = projectService;
    }

    [HttpGet]
    public async Task<ActionResult<List<ProjectResponse>>> GetUserProjects()
    {
        var userId = GetCurrentUserId();
        var projects = await _projectService.GetProjectsAsync(userId);
        return Ok(projects);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProjectDetailResponse>> GetProject(Guid id)
    {
        var userId = GetCurrentUserId();
        var project = await _projectService.GetProjectByIdAsync(id, userId);
        return Ok(project);
    }

    [HttpPost]
    public async Task<ActionResult<ProjectDetailResponse>> CreateProject([FromBody] CreateProjectRequest request)
    {
        var userId = GetCurrentUserId();
        var project = await _projectService.CreateProjectAsync(userId, request);
        return CreatedAtAction(nameof(GetProject), new { id = project.Id }, project);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ProjectDetailResponse>> UpdateProject(Guid id, [FromBody] UpdateProjectRequest request)
    {
        var userId = GetCurrentUserId();
        var project = await _projectService.UpdateProjectAsync(id, userId, request);
        return Ok(project);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteProject(Guid id)
    {
        var userId = GetCurrentUserId();
        await _projectService.DeleteProjectAsync(id, userId);
        return NoContent();
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
