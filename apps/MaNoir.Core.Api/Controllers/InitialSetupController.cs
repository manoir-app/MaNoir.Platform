using MaNoir.Core.Contracts.Models.Setup;
using MaNoir.Core.Setup;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/core/setup")]
public sealed class InitialSetupController : ControllerBase
{
    [HttpGet("status")]
    public async Task<ActionResult<InitialSetupStatus>> Status()
    {
        return Ok(await new InitialSetupLogic().GetStatusAsync(HttpContext.RequestAborted));
    }

    [HttpPost("initialize")]
    public async Task<ActionResult<InitialSetupResponse>> Initialize([FromBody] InitialSetupRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.AdminUserId) || string.IsNullOrWhiteSpace(request.AdminPassword))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["setup"] = ["The master admin user identifier and password are required."]
            }));
        }

        string graphApiBaseUri = string.Concat(Request.Scheme, "://", Request.Host.ToUriComponent());
        string machineName = Environment.MachineName;
        InitialSetupResponse response = await new InitialSetupLogic().InitializeAsync(request, graphApiBaseUri, machineName, HttpContext.RequestAborted);
        return Ok(response);
    }
}