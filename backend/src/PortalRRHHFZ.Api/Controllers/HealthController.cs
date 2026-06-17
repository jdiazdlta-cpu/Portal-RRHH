using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalRRHHFZ.Application.Common;
using PortalRRHHFZ.Infrastructure.Data;

namespace PortalRRHHFZ.Api.Controllers;

[ApiController]
[Route("api")]
public sealed class HealthController(AppDbContext db) : ControllerBase
{
    [HttpGet("health")]
    public IActionResult Health()
    {
        return Ok(ApiResponse<object>.Ok(new { status = "success", timestamp = DateTime.UtcNow }));
    }

    [HttpGet("db-test")]
    public async Task<IActionResult> DbTest(CancellationToken cancellationToken)
    {
        var canConnect = await db.Database.CanConnectAsync(cancellationToken);
        return Ok(ApiResponse<object>.Ok(new { canConnect }));
    }
}
