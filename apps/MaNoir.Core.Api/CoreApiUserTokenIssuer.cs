using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace MaNoir.Core.Api;

internal static class CoreApiUserTokenIssuer
{
    public static UserAuthenticationResponse Issue(User user, CoreApiAuthenticationOptions options)
    {
        if (user == null)
            throw new ArgumentNullException(nameof(user));

        if (options == null)
            throw new ArgumentNullException(nameof(options));

        DateTimeOffset now = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAtUtc = now.AddMinutes(options.AccessTokenLifetimeMinutes);
        string userId = UserLogic.NormalizeUserId(user.Id);
        string displayName = ResolveDisplayName(user);

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(JwtRegisteredClaimNames.UniqueName, displayName),
            new Claim(ClaimTypes.Name, displayName),
            new Claim("manoir_auth_kind", "user")
        ];

        SymmetricSecurityKey securityKey = new(Encoding.UTF8.GetBytes(options.SigningKey));
        SigningCredentials signingCredentials = new(securityKey, SecurityAlgorithms.HmacSha256);
        JwtSecurityToken token = new(
            issuer: options.Issuer,
            audience: options.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAtUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        User projectedUser = CloneUser(user);
        UserLogic.MinimizeData(projectedUser);

        return new UserAuthenticationResponse()
        {
            TokenType = "Bearer",
            AccessToken = new JwtSecurityTokenHandler().WriteToken(token),
            ExpiresAtUtc = expiresAtUtc,
            User = projectedUser
        };
    }

    private static string ResolveDisplayName(User user)
    {
        if (!string.IsNullOrWhiteSpace(user?.CommonName))
            return user.CommonName;

        if (!string.IsNullOrWhiteSpace(user?.FirstName) && !string.IsNullOrWhiteSpace(user?.Name))
            return string.Concat(user.FirstName, " ", user.Name);

        if (!string.IsNullOrWhiteSpace(user?.FirstName))
            return user.FirstName;

        if (!string.IsNullOrWhiteSpace(user?.Name))
            return user.Name;

        return UserLogic.NormalizeUserId(user?.Id);
    }

    private static User CloneUser(User user)
    {
        return new User()
        {
            Id = user.Id,
            DeleteAfter = user.DeleteAfter,
            IsGuest = user.IsGuest,
            IsAdmin = user.IsAdmin,
            IsMain = user.IsMain,
            Name = user.Name,
            FirstName = user.FirstName,
            CommonName = user.CommonName,
            SsmlTaggedName = user.SsmlTaggedName,
            HashedPinCode = user.HashedPinCode,
            HashedPassword = user.HashedPassword,
            MainEmail = user.MainEmail,
            MainPhoneNumber = user.MainPhoneNumber,
            HealthData = user.HealthData,
            Presence = user.Presence,
            Routine = user.Routine,
            Avatar = user.Avatar == null
                ? null
                : new UserImageData()
                {
                    UrlRoundBig = user.Avatar.UrlRoundBig,
                    UrlRoundSmall = user.Avatar.UrlRoundSmall,
                    UrlRoundTiny = user.Avatar.UrlRoundTiny,
                    UrlRoundSvg = user.Avatar.UrlRoundSvg,
                    UrlSquareBig = user.Avatar.UrlSquareBig,
                    UrlSquareSmall = user.Avatar.UrlSquareSmall,
                    UrlSquareTiny = user.Avatar.UrlSquareTiny,
                    UrlSquareSvg = user.Avatar.UrlSquareSvg
                }
        };
    }
}