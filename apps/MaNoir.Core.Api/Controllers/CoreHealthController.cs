using MaNoir.Core.Contracts.Models.Health;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mesh;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;

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

    [HttpGet("server-info")]
    public async Task<ActionResult<CoreServerHealthInfo>> GetServerInfo()
    {
        AutomationMesh mesh = await new AutomationMeshLogic().GetLocalAsync(HttpContext.RequestAborted);

        using Process currentProcess = Process.GetCurrentProcess();
        DateTimeOffset startedAtUtc = new(currentProcess.StartTime.ToUniversalTime(), TimeSpan.Zero);
        TimeSpan uptime = DateTimeOffset.UtcNow - startedAtUtc;

        return Ok(new CoreServerHealthInfo()
        {
            MeshName = ResolveMeshName(mesh),
            DomainName = ResolveDomainName(mesh),
            AdminUiVersion = ResolveAdminUiVersion(),
            StartedAtUtc = startedAtUtc,
            UptimeSeconds = Math.Max(0, (long)uptime.TotalSeconds)
        });
    }

    private static string ResolveAdminUiVersion()
    {
        Assembly hostAssembly = Assembly.GetEntryAssembly() ?? typeof(CoreHealthController).Assembly;
        string version = hostAssembly.GetName().Version?.ToString(3);
        if (!string.IsNullOrWhiteSpace(version))
            return version;

        AssemblyInformationalVersionAttribute informationalVersion = hostAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
        if (!string.IsNullOrWhiteSpace(informationalVersion?.InformationalVersion))
            return informationalVersion.InformationalVersion.Split('+')[0];

        return "0.0.0";
    }

    private static string ResolveMeshName(AutomationMesh mesh)
    {
        if (!string.IsNullOrWhiteSpace(mesh?.MainServer?.Name))
            return mesh.MainServer.Name;

        if (!string.IsNullOrWhiteSpace(mesh?.PublicId))
            return mesh.PublicId;

        if (!string.IsNullOrWhiteSpace(mesh?.Id))
            return mesh.Id;

        return Environment.MachineName;
    }

    private static string ResolveDomainName(AutomationMesh mesh)
    {
        if (!string.IsNullOrWhiteSpace(mesh?.PublicBaseDomain))
            return mesh.PublicBaseDomain;

        if (Uri.TryCreate(mesh?.MainServer?.MainRole?.Uri, UriKind.Absolute, out Uri uri))
            return uri.Host;

        return null;
    }
}