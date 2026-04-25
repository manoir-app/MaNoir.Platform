using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
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

        string userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
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
        string userId = User.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
            return Unauthorized();

        User user = await new UserLogic().GetByIdAsync(userId, HttpContext.RequestAborted);
        if (user == null || user.IsGuest)
            return Unauthorized();

        UserLogic.MinimizeData(user);
        return Ok(user);
    }
}