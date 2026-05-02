using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Authorization;

/// <summary>
/// Resolves and persists user access grants on functional zones.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// AuthorizationLogic logic = new AuthorizationLogic();
/// await logic.PublishAccessZoneDefinitionsAsync("manoir.core", CoreAccessZones.GetDefinitions(), cancellationToken);
/// bool canManageMesh = await logic.HasAccessAsync("michael", "core.mesh.settings", AccessLevel.Manage, cancellationToken);
/// </code>
/// </remarks>
public sealed class AuthorizationLogic
{
    private readonly AuthorizationMongoOperations _mongoOperations;
    private readonly UserLogic _userLogic;

    /// <summary>
    /// Initializes a new instance of the <see cref="AuthorizationLogic"/> class.
    /// </summary>
    public AuthorizationLogic()
    {
        _mongoOperations = new AuthorizationMongoOperations();
        _userLogic = new UserLogic();
    }

    /// <summary>
    /// Gets the published access zone definitions.
    /// </summary>
    /// <remarks>
    /// <para>Pass a plugin identifier to scope the result to one plugin, or omit it to inspect the full catalog.</para>
    /// </remarks>
    public Task<List<AccessZoneDefinition>> GetAccessZoneDefinitionsAsync(string pluginId = null, CancellationToken cancellationToken = default)
    {
        string normalizedPluginId = NormalizePluginId(pluginId);
        return _mongoOperations.GetAccessZoneDefinitionsAsync(normalizedPluginId, cancellationToken);
    }

    /// <summary>
    /// Publishes the access zone definitions of one plugin.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Republishing a plugin catalog replaces its zone set: removed definitions are deleted, and matching identifiers are updated in place.
    /// </para>
    /// </remarks>
    public async Task<List<AccessZoneDefinition>> PublishAccessZoneDefinitionsAsync(string pluginId, IEnumerable<AccessZoneDefinition> definitions, CancellationToken cancellationToken = default)
    {
        string normalizedPluginId = NormalizePluginId(pluginId);
        if (normalizedPluginId == null)
            return [];

        DateTimeOffset now = DateTimeOffset.UtcNow;
        List<AccessZoneDefinition> existingDefinitions = await _mongoOperations.GetAccessZoneDefinitionsAsync(normalizedPluginId, cancellationToken);
        List<AccessZoneDefinition> preparedDefinitions = PrepareAccessZoneDefinitions(normalizedPluginId, definitions, existingDefinitions, now);

        HashSet<string> publishedIds = new(preparedDefinitions.Select(definition => definition.Id), StringComparer.OrdinalIgnoreCase);
        foreach (AccessZoneDefinition removedDefinition in existingDefinitions.Where(definition => !publishedIds.Contains(definition.Id)))
            await _mongoOperations.DeleteAccessZoneDefinitionAsync(removedDefinition.Id, cancellationToken);

        foreach (AccessZoneDefinition definition in preparedDefinitions)
            await _mongoOperations.SaveAccessZoneDefinitionAsync(definition, cancellationToken);

        return await _mongoOperations.GetAccessZoneDefinitionsAsync(normalizedPluginId, cancellationToken);
    }

    /// <summary>
    /// Gets the explicit authorization profile of one user.
    /// </summary>
    /// <remarks>
    /// <para>Example:</para>
    /// <code>
    /// AuthorizationLogic logic = new AuthorizationLogic();
    /// UserAuthorizationProfile profile = await logic.GetUserAuthorizationAsync("michael", cancellationToken);
    /// </code>
    /// </remarks>
    public async Task<UserAuthorizationProfile> GetUserAuthorizationAsync(string userId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = UserLogic.NormalizeUserId(userId);
        if (normalizedUserId == null)
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));

        User user = await _userLogic.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user == null)
            return null;

        List<UserZoneAccess> accesses = [.. (await _mongoOperations.GetUserAccessGrantsAsync(normalizedUserId, cancellationToken))
            .Select(ToModel)
            .OrderBy(access => access.ZoneId, StringComparer.OrdinalIgnoreCase)];

        return new UserAuthorizationProfile()
        {
            UserId = normalizedUserId,
            IsMain = user.IsMain,
            IsAdmin = user.IsAdmin,
            Accesses = accesses
        };
    }

    /// <summary>
    /// Replaces the explicit authorization profile of one user.
    /// </summary>
    /// <remarks>
    /// <para>Example:</para>
    /// <code>
    /// AuthorizationLogic logic = new AuthorizationLogic();
    /// UserAuthorizationProfile profile = await logic.ReplaceUserAuthorizationAsync("michael",
    /// [
    ///     new UserZoneAccess() { ZoneId = "core.mesh.settings", Level = AccessLevel.Manage }
    /// ], cancellationToken);
    /// </code>
    /// </remarks>
    public async Task<UserAuthorizationProfile> ReplaceUserAuthorizationAsync(string userId, IEnumerable<UserZoneAccess> accesses, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = UserLogic.NormalizeUserId(userId);
        if (normalizedUserId == null)
            throw new ArgumentException("The user identifier cannot be empty.", nameof(userId));

        User user = await _userLogic.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user == null)
            throw new KeyNotFoundException("The requested user does not exist.");

        if (user.IsGuest)
            throw new ArgumentException("Guest users cannot receive explicit access grants.", nameof(userId));

        List<UserAccessGrantDocument> preparedGrants = PrepareAccessGrants(normalizedUserId, accesses);
        await _mongoOperations.ReplaceUserAccessGrantsAsync(normalizedUserId, preparedGrants, cancellationToken);

        return new UserAuthorizationProfile()
        {
            UserId = normalizedUserId,
            IsMain = user.IsMain,
            IsAdmin = user.IsAdmin,
            Accesses = [.. preparedGrants
                .Select(ToModel)
                .OrderBy(access => access.ZoneId, StringComparer.OrdinalIgnoreCase)]
        };
    }

    /// <summary>
    /// Gets the effective access level of one user on one zone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Effective access walks up the dotted zone hierarchy, so a grant on <c>core.mesh</c> can satisfy a query on <c>core.mesh.settings</c>.
    /// </para>
    /// </remarks>
    public async Task<AccessLevel> GetEffectiveAccessLevelAsync(string userId, string zoneId, CancellationToken cancellationToken = default)
    {
        string normalizedUserId = UserLogic.NormalizeUserId(userId);
        string normalizedZoneId = NormalizeZoneId(zoneId);
        if (normalizedUserId == null || normalizedZoneId == null)
            return AccessLevel.None;

        User user = await _userLogic.GetByIdAsync(normalizedUserId, cancellationToken);
        if (user == null || user.IsGuest)
            return AccessLevel.None;

        if (user.IsAdmin)
            return AccessLevel.Manage;

        List<UserAccessGrantDocument> grants = await _mongoOperations.GetUserAccessGrantsAsync(normalizedUserId, cancellationToken);
        AccessLevel effectiveLevel = AccessLevel.None;
        string candidateZoneId = normalizedZoneId;
        while (candidateZoneId != null)
        {
            UserAccessGrantDocument grant = grants.FirstOrDefault(existingGrant => string.Equals(existingGrant.ZoneId, candidateZoneId, StringComparison.Ordinal));
            if (grant != null && grant.Level > effectiveLevel)
                effectiveLevel = grant.Level;

            int lastSeparatorIndex = candidateZoneId.LastIndexOf('.');
            candidateZoneId = lastSeparatorIndex > 0
                ? candidateZoneId[..lastSeparatorIndex]
                : null;
        }

        return effectiveLevel;
    }

    /// <summary>
    /// Gets whether the user has at least the required access level on one zone.
    /// </summary>
    /// <remarks>
    /// <para>This helper is the lightweight boolean form when you only need an allow/deny answer.</para>
    /// </remarks>
    public async Task<bool> HasAccessAsync(string userId, string zoneId, AccessLevel requiredLevel, CancellationToken cancellationToken = default)
    {
        if (requiredLevel <= AccessLevel.None)
            return true;

        return await GetEffectiveAccessLevelAsync(userId, zoneId, cancellationToken) >= requiredLevel;
    }

    /// <summary>
    /// Throws when the user does not have the required access level on one zone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Use this method in API or orchestration code when unauthorized access should fail fast with an exception instead of returning a boolean.
    /// </para>
    /// </remarks>
    public async Task EnsureAccessAsync(string userId, string zoneId, AccessLevel requiredLevel, CancellationToken cancellationToken = default)
    {
        if (!await HasAccessAsync(userId, zoneId, requiredLevel, cancellationToken))
            throw new UnauthorizedAccessException("The current user does not have the required access level.");
    }

    private static List<UserAccessGrantDocument> PrepareAccessGrants(string userId, IEnumerable<UserZoneAccess> accesses)
    {
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Dictionary<string, UserAccessGrantDocument> grantsByZone = new(StringComparer.OrdinalIgnoreCase);

        foreach (UserZoneAccess access in accesses ?? [])
        {
            string normalizedZoneId = NormalizeZoneId(access?.ZoneId);
            if (normalizedZoneId == null || access.Level <= AccessLevel.None)
                continue;

            if (grantsByZone.TryGetValue(normalizedZoneId, out UserAccessGrantDocument existingGrant))
            {
                if (access.Level > existingGrant.Level)
                    existingGrant.Level = access.Level;

                existingGrant.UpdatedAtUtc = now;
                continue;
            }

            grantsByZone[normalizedZoneId] = new UserAccessGrantDocument()
            {
                Id = string.Concat(userId, ":", normalizedZoneId),
                UserId = userId,
                ZoneId = normalizedZoneId,
                Level = access.Level,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
        }

        return [.. grantsByZone.Values.OrderBy(grant => grant.ZoneId, StringComparer.OrdinalIgnoreCase)];
    }

    private static UserZoneAccess ToModel(UserAccessGrantDocument grant)
    {
        return new UserZoneAccess()
        {
            ZoneId = grant.ZoneId,
            Level = grant.Level
        };
    }

    private static string NormalizeZoneId(string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            return null;

        return zoneId.Trim().ToLowerInvariant();
    }

    private static string NormalizePluginId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;

        return pluginId.Trim().ToLowerInvariant();
    }

    private static List<AccessZoneDefinition> PrepareAccessZoneDefinitions(string pluginId, IEnumerable<AccessZoneDefinition> definitions, IEnumerable<AccessZoneDefinition> existingDefinitions, DateTimeOffset now)
    {
        Dictionary<string, AccessZoneDefinition> existingDefinitionsById = new(StringComparer.OrdinalIgnoreCase);
        foreach (AccessZoneDefinition existingDefinition in existingDefinitions ?? [])
        {
            if (existingDefinition?.Id == null)
                continue;

            existingDefinitionsById[existingDefinition.Id] = existingDefinition;
        }

        Dictionary<string, AccessZoneDefinition> preparedDefinitionsById = new(StringComparer.OrdinalIgnoreCase);
        foreach (AccessZoneDefinition definition in definitions ?? [])
        {
            string normalizedZoneId = NormalizeZoneId(definition?.Id);
            if (normalizedZoneId == null || definition == null)
                continue;

            existingDefinitionsById.TryGetValue(normalizedZoneId, out AccessZoneDefinition existingDefinition);
            preparedDefinitionsById[normalizedZoneId] = new AccessZoneDefinition()
            {
                Id = normalizedZoneId,
                PluginId = pluginId,
                Label = string.IsNullOrWhiteSpace(definition.Label) ? existingDefinition?.Label : definition.Label,
                Description = string.IsNullOrWhiteSpace(definition.Description) ? existingDefinition?.Description : definition.Description,
                PublishedAtUtc = existingDefinition?.PublishedAtUtc == default || existingDefinition == null
                    ? (definition.PublishedAtUtc == default ? now : definition.PublishedAtUtc)
                    : existingDefinition.PublishedAtUtc,
                UpdatedAtUtc = now
            };
        }

        return [.. preparedDefinitionsById.Values.OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)];
    }
}