using MaNoir.Core.Contracts.Models.Contributions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Contributions;

public sealed partial class ContributionLogic
{
    /// <summary>
    /// Gets one installed plugin by identifier.
    /// </summary>
    public Task<InstalledPlugin> GetInstalledPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        string normalizedPluginId = NormalizePluginId(pluginId);
        if (normalizedPluginId == null)
            return Task.FromResult<InstalledPlugin>(null);

        return _mongoOperations.GetInstalledPluginAsync(normalizedPluginId, cancellationToken);
    }

    /// <summary>
    /// Lists all installed plugins.
    /// </summary>
    public Task<List<InstalledPlugin>> GetInstalledPluginsAsync(CancellationToken cancellationToken = default)
    {
        return _mongoOperations.GetInstalledPluginsAsync(cancellationToken);
    }

    /// <summary>
    /// Lists installed plugins that publish at least one contribution of the requested kind.
    /// Only contributions of the requested kind are attached to the returned plugins.
    /// </summary>
    public Task<List<InstalledPlugin>> GetInstalledPluginsByContributionKindAsync(string kind, CancellationToken cancellationToken = default)
    {
        ContributionKind? normalizedKind = NormalizeContributionKind(kind);
        if (!normalizedKind.HasValue)
            return Task.FromResult<List<InstalledPlugin>>([]);

        return GetInstalledPluginsByContributionKindAsync(normalizedKind.Value, cancellationToken);
    }

    /// <summary>
    /// Lists installed plugins that publish at least one contribution of the requested kind.
    /// Only contributions of the requested kind are attached to the returned plugins.
    /// </summary>
    public async Task<List<InstalledPlugin>> GetInstalledPluginsByContributionKindAsync(ContributionKind kind, CancellationToken cancellationToken = default)
    {
        if (kind == ContributionKind.Unknown)
            return [];

        List<InstalledPlugin> installedPlugins = await _mongoOperations.GetInstalledPluginsAsync(cancellationToken);
        return [.. installedPlugins
            .Where(plugin => plugin?.Contributions != null)
            .Select(plugin => new InstalledPlugin()
            {
                Id = plugin.Id,
                Label = plugin.Label,
                Version = plugin.Version,
                Description = plugin.Description,
                Publisher = plugin.Publisher,
                IsEnabled = plugin.IsEnabled,
                IsHealthy = plugin.IsHealthy,
                InstalledAtUtc = plugin.InstalledAtUtc,
                LastSeenUtc = plugin.LastSeenUtc,
                LastPublishedAtUtc = plugin.LastPublishedAtUtc,
                LastPublishedCatalogFingerprint = plugin.LastPublishedCatalogFingerprint,
                HasNewFeatures = plugin.HasNewFeatures,
                Contributions = [.. plugin.Contributions
                    .Where(contribution => contribution != null && contribution.Kind == kind)
                    .OrderBy(contribution => contribution.Id, StringComparer.OrdinalIgnoreCase)]
            })
            .Where(plugin => plugin.Contributions.Count > 0)];
    }

    /// <summary>
    /// Gets one contribution definition by identifier.
    /// </summary>
    public Task<ContributionDefinition> GetContributionDefinitionAsync(string contributionId, CancellationToken cancellationToken = default)
    {
        string normalizedContributionId = NormalizeContributionId(contributionId);
        if (normalizedContributionId == null)
            return Task.FromResult<ContributionDefinition>(null);

        return _mongoOperations.GetContributionDefinitionAsync(normalizedContributionId, cancellationToken);
    }

    /// <summary>
    /// Lists contribution definitions optionally filtered by plugin and kind.
    /// </summary>
    public Task<List<ContributionDefinition>> GetContributionDefinitionsAsync(string pluginId = null, string kind = null, CancellationToken cancellationToken = default)
    {
        string normalizedPluginId = pluginId == null ? null : NormalizePluginId(pluginId);
        ContributionKind? normalizedKind = kind == null ? null : NormalizeContributionKind(kind);
        return _mongoOperations.GetContributionDefinitionsAsync(normalizedPluginId, normalizedKind, cancellationToken);
    }

    /// <summary>
    /// Publishes the local catalog of one installed plugin.
    /// </summary>
    public async Task<InstalledPlugin> PublishPluginCatalogAsync(InstalledPlugin plugin, IEnumerable<ContributionDefinition> definitions, CancellationToken cancellationToken = default)
    {
        string normalizedPluginId = NormalizePluginId(plugin?.Id);
        if (normalizedPluginId == null || plugin == null)
            return null;

        DateTimeOffset now = DateTimeOffset.UtcNow;
        InstalledPlugin existingPlugin = await _mongoOperations.GetInstalledPluginAsync(normalizedPluginId, cancellationToken);
        List<ContributionDefinition> existingDefinitions = existingPlugin?.Contributions ?? [];

        PrepareInstalledPluginForSave(plugin, normalizedPluginId, existingPlugin, now);
        List<ContributionDefinition> preparedDefinitions = PrepareContributionDefinitionsForSave(normalizedPluginId, definitions, existingDefinitions, now);

        string fingerprint = ComputeCatalogFingerprint(plugin, preparedDefinitions);
        plugin.HasNewFeatures = existingPlugin != null
            && !string.IsNullOrWhiteSpace(existingPlugin.LastPublishedCatalogFingerprint)
            && !string.Equals(existingPlugin.LastPublishedCatalogFingerprint, fingerprint, StringComparison.Ordinal);
        plugin.LastPublishedCatalogFingerprint = fingerprint;
        plugin.LastPublishedAtUtc = now;
        plugin.LastSeenUtc = now;
        plugin.Contributions = preparedDefinitions;

        await _mongoOperations.SaveInstalledPluginAsync(plugin, cancellationToken);
        await ArchiveInstancesForRemovedContributionsAsync(normalizedPluginId, existingDefinitions, preparedDefinitions, now, cancellationToken);

        return await _mongoOperations.GetInstalledPluginAsync(normalizedPluginId, cancellationToken);
    }

    internal static string NormalizePluginId(string pluginId)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            return null;

        return pluginId.Trim().ToLowerInvariant();
    }

    internal static string NormalizeContributionId(string contributionId)
    {
        if (string.IsNullOrWhiteSpace(contributionId))
            return null;

        return contributionId.Trim().ToLowerInvariant();
    }

    internal static ContributionKind? NormalizeContributionKind(string kind)
    {
        if (string.IsNullOrWhiteSpace(kind))
            return null;

        return kind.Trim().ToLowerInvariant() switch
        {
            "integration" => ContributionKind.Integration,
            "adminui.page" => ContributionKind.AdminUiPage,
            _ => Enum.TryParse<ContributionKind>(kind.Trim(), true, out ContributionKind parsedKind) && parsedKind != ContributionKind.Unknown
                ? parsedKind
                : null
        };
    }

    private static void PrepareInstalledPluginForSave(InstalledPlugin plugin, string normalizedPluginId, InstalledPlugin existingPlugin, DateTimeOffset now)
    {
        plugin.Id = normalizedPluginId;
        plugin.Label = string.IsNullOrWhiteSpace(plugin.Label) ? existingPlugin?.Label : plugin.Label;
        plugin.Version = string.IsNullOrWhiteSpace(plugin.Version) ? existingPlugin?.Version : plugin.Version;
        plugin.Description = string.IsNullOrWhiteSpace(plugin.Description) ? existingPlugin?.Description : plugin.Description;
        plugin.Publisher = string.IsNullOrWhiteSpace(plugin.Publisher) ? existingPlugin?.Publisher : plugin.Publisher;
        plugin.InstalledAtUtc = existingPlugin?.InstalledAtUtc == default || existingPlugin == null
            ? (plugin.InstalledAtUtc == default ? now : plugin.InstalledAtUtc)
            : existingPlugin.InstalledAtUtc;
    }

    private static List<ContributionDefinition> PrepareContributionDefinitionsForSave(string normalizedPluginId, IEnumerable<ContributionDefinition> definitions, IEnumerable<ContributionDefinition> existingDefinitions, DateTimeOffset now)
    {
        Dictionary<string, ContributionDefinition> existingDefinitionsById = new(StringComparer.OrdinalIgnoreCase);
        if (existingDefinitions != null)
        {
            foreach (ContributionDefinition existingDefinition in existingDefinitions)
            {
                if (existingDefinition?.Id == null)
                    continue;

                existingDefinitionsById[existingDefinition.Id] = existingDefinition;
            }
        }

        Dictionary<string, ContributionDefinition> preparedDefinitionsById = new(StringComparer.OrdinalIgnoreCase);
        foreach (ContributionDefinition definition in definitions ?? [])
        {
            string normalizedContributionId = NormalizeContributionId(definition?.Id);
            if (normalizedContributionId == null || definition == null || definition.Kind == ContributionKind.Unknown)
                continue;

            definition.Id = normalizedContributionId;
            definition.PluginId = normalizedPluginId;
            definition.Tags = definition.Tags == null
                ? []
                : [.. definition.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).Select(tag => tag.Trim()).Distinct(StringComparer.OrdinalIgnoreCase)];
            definition.PublishedAtUtc = existingDefinitionsById.TryGetValue(normalizedContributionId, out ContributionDefinition existingDefinition) && existingDefinition.PublishedAtUtc != default
                ? existingDefinition.PublishedAtUtc
                : (definition.PublishedAtUtc == default ? now : definition.PublishedAtUtc);
            definition.UpdatedAtUtc = now;

            if (definition.Integration != null)
            {
                definition.Integration.PublishedEntityKinds ??= [];
                foreach (IntegrationPublishedEntityKindDefinition entityKind in definition.Integration.PublishedEntityKinds)
                {
                    if (entityKind == null)
                        continue;

                    entityKind.Descriptions ??= [];
                }
            }

            if (definition.AdminUi != null)
            {
                definition.AdminUi.Pages ??= [];
                foreach (AdminUiPageDefinition page in definition.AdminUi.Pages)
                {
                    if (page == null)
                        continue;

                    page.Labels ??= [];
                }
            }

            preparedDefinitionsById[normalizedContributionId] = definition;
        }

        return [.. preparedDefinitionsById.Values.OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)];
    }

    private static string ComputeCatalogFingerprint(InstalledPlugin plugin, IReadOnlyCollection<ContributionDefinition> definitions)
    {
        object snapshot = new
        {
            Plugin = new
            {
                Id = NormalizePluginId(plugin?.Id),
                Label = plugin?.Label ?? string.Empty,
                Version = plugin?.Version ?? string.Empty,
                Description = plugin?.Description ?? string.Empty,
                Publisher = plugin?.Publisher ?? string.Empty
            },
            Definitions = definitions == null
                ? []
                : definitions
                    .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                    .Select(definition => new
                    {
                        Id = definition.Id,
                        PluginId = definition.PluginId,
                        Kind = GetContributionKindCode(definition.Kind),
                        Label = definition.Label ?? string.Empty,
                        Description = definition.Description ?? string.Empty,
                        ImageUrl = definition.ImageUrl ?? string.Empty,
                        Hidden = definition.Hidden,
                        CanCreateInstances = definition.CanCreateInstances,
                        CanInstallMultipleTimes = definition.CanInstallMultipleTimes,
                        Tags = definition.Tags == null
                            ? []
                            : definition.Tags.OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase).ToArray(),
                        Integration = definition.Integration == null
                            ? null
                            : new
                            {
                                Domain = definition.Integration.Domain ?? string.Empty,
                                Category = definition.Integration.Category ?? string.Empty,
                                ServiceDependencyKind = definition.Integration.ServiceDependencyKind,
                                RequiresExternalSubscription = definition.Integration.RequiresExternalSubscription,
                                ExternalSubscriptionInfo = definition.Integration.ExternalSubscriptionInfo ?? string.Empty,
                                DocumentationUrl = definition.Integration.DocumentationUrl ?? string.Empty,
                                PublishedEntityKinds = definition.Integration.PublishedEntityKinds == null
                                    ? []
                                    : definition.Integration.PublishedEntityKinds
                                        .OrderBy(entityKind => entityKind.Kind, StringComparer.OrdinalIgnoreCase)
                                        .Select(entityKind => new
                                        {
                                            Kind = entityKind.Kind ?? string.Empty,
                                            Descriptions = entityKind.Descriptions == null
                                                ? []
                                                : entityKind.Descriptions
                                                    .OrderBy(description => description.Key, StringComparer.OrdinalIgnoreCase)
                                                    .Select(description => new { description.Key, Value = description.Value ?? string.Empty })
                                                    .ToArray()
                                        })
                                        .ToArray()
                            },
                        AdminUi = definition.AdminUi == null
                            ? null
                            : new
                            {
                                Domain = definition.AdminUi.Domain ?? string.Empty,
                                Pages = definition.AdminUi.Pages == null
                                    ? []
                                    : definition.AdminUi.Pages
                                        .OrderBy(page => page.Name, StringComparer.OrdinalIgnoreCase)
                                        .ThenBy(page => page.Url, StringComparer.OrdinalIgnoreCase)
                                        .Select(page => new
                                        {
                                            Category = page.Category ?? string.Empty,
                                            Name = page.Name ?? string.Empty,
                                            Url = page.Url ?? string.Empty,
                                            Labels = page.Labels == null
                                                ? []
                                                : page.Labels
                                                    .OrderBy(label => label.Key, StringComparer.OrdinalIgnoreCase)
                                                    .Select(label => new { label.Key, Value = label.Value ?? string.Empty })
                                                    .ToArray()
                                        })
                                        .ToArray()
                            }
                    })
                    .ToArray()
        };

        string json = JsonConvert.SerializeObject(snapshot, Formatting.None);
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(json));
        return Convert.ToHexString(hash);
    }

    private static string GetContributionKindCode(ContributionKind kind)
    {
        return kind switch
        {
            ContributionKind.Integration => "integration",
            ContributionKind.AdminUiPage => "adminui.page",
            _ => string.Empty
        };
    }

    private async Task ArchiveInstancesForRemovedContributionsAsync(
        string pluginId,
        IReadOnlyCollection<ContributionDefinition> existingDefinitions,
        IReadOnlyCollection<ContributionDefinition> publishedDefinitions,
        DateTimeOffset now,
        CancellationToken cancellationToken)
    {
        HashSet<string> publishedDefinitionIds = publishedDefinitions == null
            ? []
            : [.. publishedDefinitions
                .Where(definition => definition?.Id != null)
                .Select(definition => definition.Id)];

        List<string> removedDefinitionIds = existingDefinitions == null
            ? []
            : [.. existingDefinitions
                .Where(definition => definition?.Id != null && !publishedDefinitionIds.Contains(definition.Id))
                .Select(definition => definition.Id)
                .Distinct(StringComparer.OrdinalIgnoreCase)];

        foreach (string removedDefinitionId in removedDefinitionIds)
        {
            List<ContributionInstance> instances = await _mongoOperations.GetContributionInstancesAsync(removedDefinitionId, cancellationToken);
            foreach (ContributionInstance instance in instances)
            {
                if (instance == null)
                    continue;

                instance.IsEnabled = false;
                instance.PluginId = pluginId;
                instance.Status = ContributionInstanceStatus.Archived;
                instance.StatusMessage = $"Archived because plugin '{pluginId}' no longer publishes contribution '{removedDefinitionId}'.";
                instance.UpdatedAtUtc = now;

                await _mongoOperations.SaveContributionInstanceAsync(instance, cancellationToken);
            }
        }
    }
}