using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Route("api/core/auth/users")]
public sealed class UserAuthenticationController : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<ActionResult<UserAuthenticationResponse>> Login([FromBody] UserLoginRequest request, [FromQuery] bool isInteractive = true)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.UserId) || string.IsNullOrWhiteSpace(request.Password))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["credentials"] = ["The user identifier and password are required."]
            }));
        }

        User user = await new UserLogic().AuthenticateByPasswordAsync(request.UserId, request.Password, HttpContext.RequestAborted);
        if (user == null)
            throw new InvalidUserCredentialsException();

        CoreApiAuthenticationOptions options = HttpContext.RequestServices.GetRequiredService<CoreApiAuthenticationOptions>();
        UserAuthenticationResponse response = CoreApiUserTokenIssuer.Issue(user, options);
        if (isInteractive)
        {
            Response.Cookies.Append(options.CookieName, response.AccessToken, new CookieOptions()
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Secure = Request.IsHttps,
                Path = "/",
                Expires = response.ExpiresAtUtc
            });

            response.AccessToken = null;
        }

        return Ok(response);
    }

    [Authorize]
    [HttpPost("change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] UserChangePasswordRequest request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.CurrentPassword) || string.IsNullOrWhiteSpace(request.NewPassword))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["password"] = ["The current password and the new password are required."]
            }));
        }

        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        await new UserLogic().ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, HttpContext.RequestAborted);
        return NoContent();
    }

    [AllowAnonymous]
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        CoreApiAuthenticationOptions options = HttpContext.RequestServices.GetRequiredService<CoreApiAuthenticationOptions>();
        Response.Cookies.Delete(options.CookieName, new CookieOptions()
        {
            HttpOnly = true,
            IsEssential = true,
            SameSite = SameSiteMode.Lax,
            Secure = Request.IsHttps,
            Path = "/"
        });

        return NoContent();
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<ActionResult<User>> Me()
    {
        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        User user = await new UserLogic().GetByIdAsync(userId, HttpContext.RequestAborted);
        if (user == null || user.IsGuest)
            return Unauthorized();

        UserLogic.MinimizeData(user);
        return Ok(user);
    }

    [Authorize]
    [HttpGet("admin")]
    public async Task<ActionResult<User>> GetCurrentAdmin()
    {
        User adminUser = await new UserLogic().GetAdminUserAsync(HttpContext.RequestAborted);
        if (adminUser == null)
            return NotFound();

        UserLogic.MinimizeData(adminUser);
        return Ok(adminUser);
    }

    [Authorize]
    [HttpGet("me/access")]
    public async Task<ActionResult<UserAuthorizationProfile>> GetMyAccess()
    {
        string userId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        UserAuthorizationProfile profile = await new AuthorizationLogic().GetUserAuthorizationAsync(userId, HttpContext.RequestAborted);
        return profile == null ? Unauthorized() : Ok(profile);
    }

    [Authorize]
    [HttpGet("access-zones")]
    public async Task<ActionResult<List<AccessZoneDefinition>>> GetAccessZones([FromQuery] string pluginId = null)
    {
        if (!await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreAuthorization, AccessLevel.Manage))
            return Unauthorized();

        return Ok(await new AuthorizationLogic().GetAccessZoneDefinitionsAsync(pluginId, HttpContext.RequestAborted));
    }

    [Authorize]
    [HttpGet("{userId}/access")]
    public async Task<ActionResult<UserAuthorizationProfile>> GetUserAccess(string userId)
    {
        if (!await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreAuthorization, AccessLevel.Manage))
            return Unauthorized();

        UserAuthorizationProfile profile = await new AuthorizationLogic().GetUserAuthorizationAsync(userId, HttpContext.RequestAborted);
        return profile == null ? NotFound() : Ok(profile);
    }

    [Authorize]
    [HttpPut("{userId}/access")]
    public async Task<ActionResult<UserAuthorizationProfile>> ReplaceUserAccess(string userId, [FromBody] UserAuthorizationUpdateRequest request)
    {
        if (!await EnsureCurrentUserAccessAsync(CoreAccessZones.CoreAuthorization, AccessLevel.Manage))
            return Unauthorized();

        if (request == null)
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
            {
                ["accesses"] = ["The authorization payload is required."]
            }));
        }

        UserAuthorizationProfile profile = await new AuthorizationLogic().ReplaceUserAuthorizationAsync(userId, request.Accesses, HttpContext.RequestAborted);
        return Ok(profile);
    }

    [Authorize]
    [HttpPost("{userId}/admin")]
    public async Task<ActionResult<User>> ChangeAdminUser(string userId)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return Unauthorized();

        User currentUser = await new UserLogic().GetByIdAsync(currentUserId, HttpContext.RequestAborted);
        if (currentUser == null || !currentUser.IsAdmin)
            return Forbid();

        User updatedUser = await new UserLogic().ChangeAdminUserAsync(userId, HttpContext.RequestAborted);
        if (updatedUser == null)
            return NotFound();

        UserLogic.MinimizeData(updatedUser);
        return Ok(updatedUser);
    }

    private async Task<bool> EnsureCurrentUserAccessAsync(string zoneId, AccessLevel requiredLevel)
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            return false;

        await new AuthorizationLogic().EnsureAccessAsync(currentUserId, zoneId, requiredLevel, HttpContext.RequestAborted);
        return true;
    }
}