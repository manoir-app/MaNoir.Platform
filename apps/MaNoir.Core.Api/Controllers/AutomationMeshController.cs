using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Mesh;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Route("api/core/system/mesh")]
public sealed class AutomationMeshController : ControllerBase
{
    [AllowAnonymous]
    [HttpGet("local/settings")]
    public async Task<ActionResult<AutomationMeshLocalSettings>> GetLocalSettings()
    {
        AutomationMesh mesh = await new AutomationMeshLogic().GetLocalAsync(HttpContext.RequestAborted);
        if (mesh == null)
            return NotFound();

        return Ok(new AutomationMeshLocalSettings()
        {
            MeshId = mesh.Id,
            PublicId = mesh.PublicId,
            PublicBaseDomain = mesh.PublicBaseDomain,
            LanguageId = mesh.LanguageId,
            TimeZoneId = mesh.TimeZoneId,
            CountryId = mesh.CountryId
        });
    }

    [AllowAnonymous]
    [HttpGet("local/frontends")]
    public async Task<ActionResult<Dictionary<string, string>>> GetLocalFrontendUrls()
    {
        return Ok(await new AutomationMeshLogic().GetFrontendUrlsAsync(HttpContext.RequestAborted));
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpPut("local/frontends/{frontendCode}")]
    public async Task<IActionResult> PutLocalFrontendUrl(string frontendCode, [FromBody] AutomationMeshFrontendUrlUpsertRequest request)
    {
        await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreMeshSettings, AccessLevel.Manage);

        string normalizedFrontendCode = AutomationMeshLogic.NormalizeFrontendCode(frontendCode);
        if (normalizedFrontendCode == null)
            return CreateInvalidFrontendResponse("frontendCode", "The frontend code is required.");

        string normalizedFrontendUrl = AutomationMeshLogic.NormalizeFrontendUrl(request?.Url);
        if (normalizedFrontendUrl == null)
            return CreateInvalidFrontendResponse("url", "The frontend URL must be an absolute URL.");

        AutomationMeshLogic logic = new AutomationMeshLogic();
        if (await logic.GetLocalAsync(HttpContext.RequestAborted) == null)
            return NotFound();

        await logic.SetFrontendUrlAsync(normalizedFrontendCode, normalizedFrontendUrl, HttpContext.RequestAborted);
        return NoContent();
    }

    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpDelete("local/frontends/{frontendCode}")]
    public async Task<IActionResult> DeleteLocalFrontendUrl(string frontendCode)
    {
        await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreMeshSettings, AccessLevel.Manage);

        string normalizedFrontendCode = AutomationMeshLogic.NormalizeFrontendCode(frontendCode);
        if (normalizedFrontendCode == null)
            return CreateInvalidFrontendResponse("frontendCode", "The frontend code is required.");

        AutomationMeshLogic logic = new AutomationMeshLogic();
        if (await logic.GetLocalAsync(HttpContext.RequestAborted) == null)
            return NotFound();

        bool deleted = await logic.DeleteFrontendUrlAsync(normalizedFrontendCode, HttpContext.RequestAborted);
        return deleted ? NoContent() : NotFound();
    }

    private async Task EnsureCurrentUserAccessAsync(string zoneId, AccessLevel requiredLevel)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UnauthorizedAccessException("An authenticated user is required.");

        await new AuthorizationLogic().EnsureAccessAsync(currentUserId, zoneId, requiredLevel, HttpContext.RequestAborted);
    }

    private BadRequestObjectResult CreateInvalidFrontendResponse(string fieldName, string message)
    {
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
        {
            [fieldName] = [message]
        }));
    }
}