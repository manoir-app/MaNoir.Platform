using MaNoir.Core.Observability;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/core/system/logs")]
public sealed class PlatformLogsController : ControllerBase
{
    [HttpGet("services")]
    public async Task<ActionResult<List<string>>> GetServices()
    {
        return Ok(await new LokiLogBrowserLogic().GetServiceNamesAsync(HttpContext.RequestAborted));
    }

    [HttpGet("entries")]
    public async Task<ActionResult<LokiLogQueryResponse>> GetEntries(
        [FromQuery] string serviceName = null,
        [FromQuery] string contains = null,
        [FromQuery] int limit = 200,
        [FromQuery] string direction = "backward",
        [FromQuery] DateTimeOffset? startUtc = null,
        [FromQuery] DateTimeOffset? endUtc = null)
    {
        return Ok(await new LokiLogBrowserLogic().QueryAsync(
            serviceName,
            contains,
            limit,
            direction,
            startUtc,
            endUtc,
            HttpContext.RequestAborted));
    }
}