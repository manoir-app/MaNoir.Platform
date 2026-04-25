using MaNoir.Core.Contracts.Models.Entities;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Users;

/// <summary>
/// Projects non-guest users as read-only entities.
/// </summary>
public sealed class UserProjectedEntityRepository : IProjectedEntityRepository
{
    /// <inheritdoc/>
    public string Source => "users/catalog";

    /// <inheritdoc/>
    public IReadOnlyCollection<string> SupportedKinds => [UserEntityConstants.Kinds.User];

    /// <inheritdoc/>
    public async Task<Entity> GetByIdAsync(string kind, string entityId, CancellationToken cancellationToken = default)
    {
        string normalizedKind = EntityLogic.NormalizeEntityKind(kind);
        string normalizedEntityId = EntityLogic.NormalizeEntityId(entityId);
        if (normalizedKind != UserEntityConstants.Kinds.User || normalizedEntityId == null)
            return null;

        User user = await new UserLogic().GetByIdAsync(normalizedEntityId, cancellationToken);
        if (user == null || user.IsGuest)
            return null;

        return ToProjectedEntity(user);
    }

    /// <inheritdoc/>
    public async Task<List<Entity>> GetByKindsAsync(IReadOnlyCollection<string> kinds, CancellationToken cancellationToken = default)
    {
        List<string> normalizedKinds = EntityLogic.NormalizeEntityKinds(kinds);
        if (!normalizedKinds.Contains(UserEntityConstants.Kinds.User))
            return [];

        List<User> users = await new UserLogic().GetNonGuestUsersAsync(cancellationToken);
        List<Entity> entities = [];

        foreach (User user in users)
        {
            Entity entity = ToProjectedEntity(user);
            if (entity != null)
                entities.Add(entity);
        }

        return entities;
    }

    private static Entity ToProjectedEntity(User user)
    {
        string userId = UserLogic.NormalizeUserId(user?.Id);
        if (user == null || user.IsGuest || userId == null)
            return null;

        string displayName = ResolveDisplayName(user);
        string avatarUrl = ResolveAvatarUrl(user.Avatar);

        Entity entity = new Entity()
        {
            Id = userId,
            EntityKind = UserEntityConstants.Kinds.User,
            Name = displayName,
            DefaultImageUrl = avatarUrl,
            CurrentImageUrl = avatarUrl,
            Datas =
            {
                ["DisplayName"] = CreateData(displayName, UserEntityConstants.Categories.Identity),
                ["IsMain"] = CreateData(user.IsMain ? "true" : "false", UserEntityConstants.Categories.Flags)
            }
        };

        AddIfPresent(entity, "FirstName", user.FirstName, UserEntityConstants.Categories.Identity);
        AddIfPresent(entity, "Name", user.Name, UserEntityConstants.Categories.Identity);
        AddIfPresent(entity, "CommonName", user.CommonName, UserEntityConstants.Categories.Identity);
        AddIfPresent(entity, "SsmlTaggedName", user.SsmlTaggedName, UserEntityConstants.Categories.Identity);

        return entity;
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

    private static string ResolveAvatarUrl(UserImageData avatar)
    {
        if (avatar == null)
            return null;

        return avatar.UrlRoundBig
            ?? avatar.UrlSquareBig
            ?? avatar.UrlRoundSmall
            ?? avatar.UrlSquareSmall
            ?? avatar.UrlRoundTiny
            ?? avatar.UrlSquareTiny
            ?? avatar.UrlRoundSvg
            ?? avatar.UrlSquareSvg;
    }

    private static void AddIfPresent(Entity entity, string key, string value, string category)
    {
        if (entity == null || string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
            return;

        entity.Datas[key] = CreateData(value, category);
    }

    private static EntityData CreateData(string value, string category)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        return new EntityData()
        {
            SimpleType = "System.String",
            SimpleValue = value,
            Category = category
        };
    }
}