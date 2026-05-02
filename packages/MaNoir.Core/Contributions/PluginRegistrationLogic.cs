using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Contributions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Publishes complete plugin descriptors made of plugin metadata, contributions, and access zones.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// PluginRegistrationLogic logic = new PluginRegistrationLogic();
/// await logic.PublishPluginDescriptorAsync(CorePluginDescriptorProvider.Create("1.0.0"), cancellationToken);
/// </code>
/// <para>
/// This is the preferred entry point when a plugin wants to publish its catalog, contribution surfaces,
/// and access zones as one coherent operation.
/// </para>
/// </remarks>
public sealed class PluginRegistrationLogic
{
    /// <summary>
    /// Publishes one complete plugin descriptor.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Repository dependencies are validated before publication so that contribution access-zone references stay resolvable.
    /// The method throws <see cref="InvalidPluginDescriptorException"/> when the descriptor is inconsistent.
    /// </para>
    /// </remarks>
    public async Task PublishPluginDescriptorAsync(PluginDescriptor pluginDescriptor, CancellationToken cancellationToken = default)
    {
        if (pluginDescriptor == null)
            return;

        await ValidateAccessZoneReferencesAsync(pluginDescriptor, cancellationToken);

        await new ContributionLogic().PublishPluginCatalogAsync(new InstalledPlugin()
        {
            Id = pluginDescriptor.Id,
            Label = pluginDescriptor.Label,
            Version = pluginDescriptor.Version,
            Description = pluginDescriptor.Description,
            Publisher = pluginDescriptor.Publisher,
            RepositoryUrl = pluginDescriptor.RepositoryUrl,
            DependencyRepositoryUrls = pluginDescriptor.DependencyRepositoryUrls
        }, pluginDescriptor.Contributions, cancellationToken);

        await new AuthorizationLogic().PublishAccessZoneDefinitionsAsync(pluginDescriptor.Id, pluginDescriptor.AccessZones, cancellationToken);
    }

    private static async Task ValidateAccessZoneReferencesAsync(PluginDescriptor pluginDescriptor, CancellationToken cancellationToken)
    {
        HashSet<string> allowedZoneIds = new((pluginDescriptor.AccessZones ?? [])
            .Select(definition => NormalizeAccessZoneId(definition?.Id))
            .Where(zoneId => zoneId != null), StringComparer.OrdinalIgnoreCase);

        List<string> dependencyRepositoryUrls = pluginDescriptor.DependencyRepositoryUrls == null
            ? []
            : [.. pluginDescriptor.DependencyRepositoryUrls
                .Select(ContributionLogic.NormalizeRepositoryUrl)
                .Where(repositoryUrl => repositoryUrl != null)
                .Distinct(StringComparer.OrdinalIgnoreCase)];

        if (dependencyRepositoryUrls.Count > 0)
        {
            ContributionLogic contributionLogic = new ContributionLogic();
            AuthorizationLogic authorizationLogic = new AuthorizationLogic();
            List<InstalledPlugin> installedPlugins = await contributionLogic.GetInstalledPluginsAsync(cancellationToken);
            string currentPluginRepositoryUrl = ContributionLogic.NormalizeRepositoryUrl(pluginDescriptor.RepositoryUrl);
            if (currentPluginRepositoryUrl != null)
            {
                installedPlugins.RemoveAll(plugin => plugin != null && string.Equals(currentPluginRepositoryUrl, ContributionLogic.NormalizeRepositoryUrl(plugin.RepositoryUrl), StringComparison.OrdinalIgnoreCase));
                installedPlugins.Add(new InstalledPlugin()
                {
                    Id = pluginDescriptor.Id,
                    RepositoryUrl = currentPluginRepositoryUrl,
                    DependencyRepositoryUrls = pluginDescriptor.DependencyRepositoryUrls == null
                        ? []
                        : [.. pluginDescriptor.DependencyRepositoryUrls]
                });
            }

            Dictionary<string, List<InstalledPlugin>> pluginsByRepositoryUrl = installedPlugins
                .Where(plugin => plugin != null)
                .GroupBy(plugin => ContributionLogic.NormalizeRepositoryUrl(plugin.RepositoryUrl), StringComparer.OrdinalIgnoreCase)
                .Where(group => group.Key != null)
                .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.OrdinalIgnoreCase);
            HashSet<string> visitedRepositoryUrls = new(StringComparer.OrdinalIgnoreCase);

            foreach (string dependencyRepositoryUrl in dependencyRepositoryUrls)
                await CollectDependencyAccessZoneIdsAsync(dependencyRepositoryUrl, pluginsByRepositoryUrl, authorizationLogic, allowedZoneIds, visitedRepositoryUrls, [], cancellationToken);
        }

        foreach (ContributionDefinition contribution in pluginDescriptor.Contributions ?? [])
        {
            string normalizedZoneId = NormalizeAccessZoneId(contribution?.AdminUi?.AccessZoneId);
            if (normalizedZoneId != null && !allowedZoneIds.Contains(normalizedZoneId))
                throw new InvalidPluginDescriptorException($"The contribution '{contribution?.Id}' references access zone '{normalizedZoneId}' but this zone is neither published by the plugin nor by one of its declared repository dependencies.");
        }
    }

    private static string NormalizeAccessZoneId(string zoneId)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
            return null;

        return zoneId.Trim().ToLowerInvariant();
    }

    private static async Task CollectDependencyAccessZoneIdsAsync(
        string repositoryUrl,
        IReadOnlyDictionary<string, List<InstalledPlugin>> pluginsByRepositoryUrl,
        AuthorizationLogic authorizationLogic,
        ISet<string> allowedZoneIds,
        ISet<string> visitedRepositoryUrls,
        IReadOnlyCollection<string> traversalPath,
        CancellationToken cancellationToken)
    {
        if (repositoryUrl == null)
            return;

        if (traversalPath.Contains(repositoryUrl, StringComparer.OrdinalIgnoreCase))
        {
            string cyclePath = string.Join(" -> ", traversalPath.Append(repositoryUrl));
            throw new InvalidPluginDescriptorException($"The plugin repository dependency graph contains a cycle: {cyclePath}.");
        }

        if (!visitedRepositoryUrls.Add(repositoryUrl))
            return;

        if (!pluginsByRepositoryUrl.TryGetValue(repositoryUrl, out List<InstalledPlugin> dependencyPlugins))
            return;

        List<string> nextTraversalPath = [.. traversalPath, repositoryUrl];
        foreach (InstalledPlugin dependencyPlugin in dependencyPlugins)
        {
            foreach (AccessZoneDefinition definition in await authorizationLogic.GetAccessZoneDefinitionsAsync(dependencyPlugin.Id, cancellationToken))
            {
                string normalizedZoneId = NormalizeAccessZoneId(definition?.Id);
                if (normalizedZoneId != null)
                    allowedZoneIds.Add(normalizedZoneId);
            }

            foreach (string parentRepositoryUrl in dependencyPlugin.DependencyRepositoryUrls ?? [])
            {
                string normalizedParentRepositoryUrl = ContributionLogic.NormalizeRepositoryUrl(parentRepositoryUrl);
                if (normalizedParentRepositoryUrl != null)
                    await CollectDependencyAccessZoneIdsAsync(normalizedParentRepositoryUrl, pluginsByRepositoryUrl, authorizationLogic, allowedZoneIds, visitedRepositoryUrls, nextTraversalPath, cancellationToken);
            }
        }
    }
}