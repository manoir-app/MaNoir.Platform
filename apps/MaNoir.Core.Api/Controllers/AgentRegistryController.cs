using MaNoir.Core.Agents;
using MaNoir.Core.Contracts.Models.Agents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Route("api/core/system/agents")]
public sealed class AgentRegistryController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<List<RegisteredAgent>>> GetAgents([FromQuery] string meshId = null)
    {
        return Ok(await new AgentRegistryLogic().GetAgentsAsync(meshId, HttpContext.RequestAborted));
    }

    [HttpGet("{agentId}")]
    public async Task<ActionResult<RegisteredAgent>> GetAgent(string agentId, [FromQuery] string meshId = "local")
    {
        RegisteredAgent agent = await new AgentRegistryLogic().GetAgentAsync(meshId, agentId, HttpContext.RequestAborted);
        return agent == null ? NotFound() : Ok(agent);
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<RegisteredAgent>> Register([FromBody] AgentRegistrationRequest request)
    {
        EnsureValidApiKey();
        RegisteredAgent agent = await new AgentRegistryLogic().RegisterAsync(request, HttpContext.RequestAborted);
        return Ok(agent);
    }

    [AllowAnonymous]
    [HttpPost("heartbeat")]
    public async Task<ActionResult<RegisteredAgent>> Heartbeat([FromBody] AgentHeartbeatRequest request)
    {
        EnsureValidApiKey();
        RegisteredAgent agent = await new AgentRegistryLogic().HeartbeatAsync(request, HttpContext.RequestAborted);
        return Ok(agent);
    }

    private string ResolveApiKey()
    {
        string headerValue = Request.Headers["X-Api-Key"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(headerValue))
            return headerValue;

        string queryValue = Request.Query["apiKey"].FirstOrDefault();
        return string.IsNullOrWhiteSpace(queryValue) ? null : queryValue;
    }

    private void EnsureValidApiKey()
    {
        string expectedApiKey = Environment.GetEnvironmentVariable("MANOIR_APIKEY");
        if (string.IsNullOrWhiteSpace(expectedApiKey))
            expectedApiKey = Environment.GetEnvironmentVariable("HOMEAUTOMATION_APIKEY");

        if (string.IsNullOrWhiteSpace(expectedApiKey))
            throw new InvalidOperationException("Agent registry authentication requires MANOIR_APIKEY or HOMEAUTOMATION_APIKEY to be configured.");

        if (!string.Equals(expectedApiKey, ResolveApiKey(), StringComparison.Ordinal))
            throw new UnauthorizedAccessException("The provided API key is invalid.");
    }
}