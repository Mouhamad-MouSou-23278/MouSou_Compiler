using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OnlineCompiler.Application.Common.Interfaces;

namespace OnlineCompiler.Web.Controllers;

[ApiController]
public class HealthController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public HealthController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet("healthz")]
    [HttpGet("api/health")]
    public async Task<IActionResult> GetHealth()
    {
        bool dbHealthy = false;
        try
        {
            dbHealthy = await _context.Users.AnyAsync() || true;
        }
        catch
        {
            dbHealthy = false;
        }

        var process = Process.GetCurrentProcess();

        var healthData = new
        {
            status = dbHealthy ? "Healthy" : "Degraded",
            timestamp = DateTime.UtcNow,
            version = "2.0.0",
            checks = new
            {
                database = dbHealthy ? "Connected" : "Disconnected",
                processUptime = DateTime.UtcNow - process.StartTime.ToUniversalTime(),
                memoryWorkingSetMb = process.WorkingSet64 / (1024 * 1024)
            }
        };

        return dbHealthy ? Ok(healthData) : StatusCode(503, healthData);
    }
}
