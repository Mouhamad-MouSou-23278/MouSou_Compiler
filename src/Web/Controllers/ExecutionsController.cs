using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCompiler.Application.DTOs.Executions;
using OnlineCompiler.Application.Interfaces;

namespace OnlineCompiler.Web.Controllers;

[Authorize]
[ApiController]
[Route("api")]
public class ExecutionsController : ControllerBase
{
    private readonly IExecutionService _executionService;

    public ExecutionsController(IExecutionService executionService)
    {
        _executionService = executionService;
    }

    [HttpPost("executions")]
    public async Task<ActionResult<ExecutionResponse>> RunExecution([FromBody] RunExecutionRequest request)
    {
        var userId = GetCurrentUserId();
        var response = await _executionService.EnqueueExecutionAsync(userId, request);
        return AcceptedAtAction(nameof(GetExecution), new { id = response.Id }, response);
    }

    [HttpGet("executions/{id:guid}")]
    public async Task<ActionResult<ExecutionDetailResponse>> GetExecution(Guid id)
    {
        var userId = GetCurrentUserId();
        var execution = await _executionService.GetExecutionByIdAsync(id, userId);
        return Ok(execution);
    }

    [HttpGet("projects/{projectId:guid}/executions")]
    public async Task<ActionResult<List<ExecutionResponse>>> GetProjectExecutions(Guid projectId, [FromQuery] int limit = 20)
    {
        var userId = GetCurrentUserId();
        var executions = await _executionService.GetProjectExecutionsAsync(projectId, userId, limit);
        return Ok(executions);
    }

    [HttpPost("executions/{id:guid}/input")]
    public async Task<IActionResult> SendInput(Guid id, [FromBody] SendInputRequest request)
    {
        var userId = GetCurrentUserId();
        await _executionService.SendInputAsync(id, userId, request.Input);
        return Ok(new { message = "Stdin forwarded." });
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
