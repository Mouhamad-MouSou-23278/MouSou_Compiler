using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using OnlineCompiler.Application.DTOs.Executions;
using OnlineCompiler.Application.Interfaces;

namespace OnlineCompiler.Web.Hubs;

public class ExecutionHub : Hub
{
    private readonly IExecutionService _executionService;

    public ExecutionHub(IExecutionService executionService)
    {
        _executionService = executionService;
    }

    public async Task JoinExecution(string executionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"exec-{executionId}");
    }

    public async Task LeaveExecution(string executionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"exec-{executionId}");
    }

    public async Task SendInput(string executionId, string input)
    {
        if (Guid.TryParse(executionId, out var id))
        {
            var userIdStr = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userId = Guid.TryParse(userIdStr, out var uId) ? uId : Guid.Empty;

            await _executionService.SendInputAsync(id, userId, input);
        }
    }
}
