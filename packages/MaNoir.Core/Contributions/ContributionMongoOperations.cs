using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Provides MongoDB-backed operations for installed plugins and contribution instances.
/// </summary>
public sealed class ContributionMongoOperations
{
    private readonly IMongoCollection<InstalledPlugin> _pluginCollection;
    private readonly IMongoCollection<ContributionInstance> _instanceCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContributionMongoOperations"/> class.
    /// </summary>
    public ContributionMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _pluginCollection = mongo.GetCollection<InstalledPlugin>();
        _instanceCollection = mongo.GetCollection<ContributionInstance>();
    }

    /// <summary>
    /// Gets an installed plugin by identifier.
    /// </summary>
    public Task<InstalledPlugin> GetInstalledPluginAsync(string pluginId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pluginId))
            throw new ArgumentException("The plugin identifier cannot be empty.", nameof(pluginId));

        return GetInstalledPluginCoreAsync(pluginId, cancellationToken);
    }

    /// <summary>
    /// Lists all installed plugins.
    /// </summary>
    public async Task<List<InstalledPlugin>> GetInstalledPluginsAsync(CancellationToken cancellationToken = default)
    {
        List<InstalledPlugin> plugins = await _pluginCollection.Find(plugin => true).ToListAsync(cancellationToken);
        foreach (InstalledPlugin plugin in plugins)
            plugin.Contributions ??= [];

        return plugins;
    }

    /// <summary>
    /// Inserts or replaces an installed plugin by identifier.
    /// </summary>
    public Task SaveInstalledPluginAsync(InstalledPlugin plugin, CancellationToken cancellationToken = default)
    {
        if (plugin == null)
            throw new ArgumentNullException(nameof(plugin));

        if (string.IsNullOrWhiteSpace(plugin.Id))
            throw new ArgumentException("The plugin identifier cannot be empty.", nameof(plugin));

        plugin.Contributions ??= [];

        return _pluginCollection.ReplaceOneAsync(
            existingPlugin => existingPlugin.Id == plugin.Id,
            plugin,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Gets a contribution definition by identifier.
    /// </summary>
    public Task<ContributionDefinition> GetContributionDefinitionAsync(string contributionId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(contributionId))
            throw new ArgumentException("The contribution identifier cannot be empty.", nameof(contributionId));

        return GetContributionDefinitionCoreAsync(contributionId, cancellationToken);
    }

    /// <summary>
    /// Lists contribution definitions optionally filtered by plugin and kind.
    /// </summary>
    public async Task<List<ContributionDefinition>> GetContributionDefinitionsAsync(string pluginId = null, ContributionKind? kind = null, CancellationToken cancellationToken = default)
    {
        List<InstalledPlugin> plugins;
        if (!string.IsNullOrWhiteSpace(pluginId))
        {
            InstalledPlugin plugin = await GetInstalledPluginCoreAsync(pluginId, cancellationToken);
            plugins = plugin == null ? [] : [plugin];
        }
        else
        {
            plugins = await GetInstalledPluginsAsync(cancellationToken);
        }

        IEnumerable<ContributionDefinition> definitions = plugins
            .Where(plugin => plugin?.Contributions != null)
            .SelectMany(plugin => plugin.Contributions)
            .Where(definition => definition != null);

        if (kind.HasValue)
            definitions = definitions.Where(definition => definition.Kind == kind.Value);

        return [.. definitions.OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)];
    }

    /// <summary>
    /// Gets one contribution instance by identifier.
    /// </summary>
    public Task<ContributionInstance> GetContributionInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("The contribution instance identifier cannot be empty.", nameof(instanceId));

        return _instanceCollection.Find(instance => instance.Id == instanceId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Lists contribution instances optionally filtered by definition identifier.
    /// </summary>
    public Task<List<ContributionInstance>> GetContributionInstancesAsync(string contributionDefinitionId = null, CancellationToken cancellationToken = default)
    {
        FilterDefinitionBuilder<ContributionInstance> filterBuilder = Builders<ContributionInstance>.Filter;
        FilterDefinition<ContributionInstance> filter = FilterDefinition<ContributionInstance>.Empty;

        if (!string.IsNullOrWhiteSpace(contributionDefinitionId))
            filter &= filterBuilder.Eq(instance => instance.ContributionDefinitionId, contributionDefinitionId);

        return _instanceCollection.Find(filter).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces one contribution instance by identifier.
    /// </summary>
    public Task SaveContributionInstanceAsync(ContributionInstance instance, CancellationToken cancellationToken = default)
    {
        if (instance == null)
            throw new ArgumentNullException(nameof(instance));

        if (string.IsNullOrWhiteSpace(instance.Id))
            throw new ArgumentException("The contribution instance identifier cannot be empty.", nameof(instance));

        return _instanceCollection.ReplaceOneAsync(
            existingInstance => existingInstance.Id == instance.Id,
            instance,
            new ReplaceOptions() { IsUpsert = true },
            cancellationToken);
    }

    /// <summary>
    /// Deletes one contribution instance by identifier.
    /// </summary>
    public Task<DeleteResult> DeleteContributionInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            throw new ArgumentException("The contribution instance identifier cannot be empty.", nameof(instanceId));

        return _instanceCollection.DeleteOneAsync(instance => instance.Id == instanceId, cancellationToken);
    }

    private async Task<InstalledPlugin> GetInstalledPluginCoreAsync(string pluginId, CancellationToken cancellationToken)
    {
        InstalledPlugin plugin = await _pluginCollection.Find(existingPlugin => existingPlugin.Id == pluginId).FirstOrDefaultAsync(cancellationToken);
        if (plugin != null)
            plugin.Contributions ??= [];

        return plugin;
    }

    private async Task<ContributionDefinition> GetContributionDefinitionCoreAsync(string contributionId, CancellationToken cancellationToken)
    {
        List<InstalledPlugin> plugins = await GetInstalledPluginsAsync(cancellationToken);
        return plugins
            .Where(plugin => plugin?.Contributions != null)
            .SelectMany(plugin => plugin.Contributions)
            .FirstOrDefault(definition => string.Equals(definition?.Id, contributionId, StringComparison.Ordinal));
    }
}