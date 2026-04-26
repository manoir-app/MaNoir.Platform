using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace MaNoir.Core.Api;

internal static class CoreApiUserContext
{
    public static string GetUserId(ClaimsPrincipal user)
    {
        return user?.FindFirstValue(JwtRegisteredClaimNames.Sub) ?? user?.FindFirstValue(ClaimTypes.NameIdentifier);
    }

    public static string GetUserId(ControllerBase controller)
    {
        return controller == null ? null : GetUserId(controller.User);
    }
}