using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/core/health")]
public sealed class CoreHealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { status = "ok" });
    }
}