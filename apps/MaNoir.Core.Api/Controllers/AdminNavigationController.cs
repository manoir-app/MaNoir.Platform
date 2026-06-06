using MaNoir.Core.AdminNavigation;
using MaNoir.Core.Api;
using MaNoir.Core.Contracts.Models.AdminUi;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/core/system/admin-navigation")]
public sealed class AdminNavigationController : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AdminNavigationDomainsResponse>> GetDomains()
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        AdminNavigationDomainsResponse response = await new AdminNavigationLogic().GetDomainsAsync(currentUserId, HttpContext.RequestAborted);
        return Ok(response);
    }

    [HttpGet("domains/{domainId}")]
    public async Task<ActionResult<AdminDomainNavigationResponse>> GetDomain(string domainId)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        AdminDomainNavigationResponse response = await new AdminNavigationLogic().GetDomainAsync(currentUserId, domainId, HttpContext.RequestAborted);
        return response == null ? NotFound() : Ok(response);
    }
}