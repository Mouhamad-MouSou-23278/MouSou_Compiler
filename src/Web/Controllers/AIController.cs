using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineCompiler.Application.DTOs.AI;
using OnlineCompiler.Application.Interfaces;

namespace OnlineCompiler.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly IAIFacadeService _aiFacadeService;

    public AIController(IAIFacadeService aiFacadeService)
    {
        _aiFacadeService = aiFacadeService;
    }

    [HttpPost("analyze")]
    public async Task<ActionResult<AIAnalyzeResponse>> Analyze([FromBody] AIAnalyzeRequest request)
    {
        var userId = GetCurrentUserId();
        var response = await _aiFacadeService.AnalyzeAsync(userId, request);
        return Ok(response);
    }

    [HttpPost("auto-fix")]
    public async Task<ActionResult<AIAutoFixResponse>> AutoFix([FromBody] AIAutoFixRequest request)
    {
        var userId = GetCurrentUserId();
        var response = await _aiFacadeService.AutoFixAsync(userId, request);
        return Ok(response);
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
