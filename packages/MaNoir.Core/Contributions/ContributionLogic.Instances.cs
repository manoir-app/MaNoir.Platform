using MaNoir.Core.Contracts.Models.Contributions;
using Home.Common;
using Home.Common.Messages;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Contributions;

public sealed partial class ContributionLogic
{
    /// <summary>
    /// Gets one contribution instance by identifier.
    /// </summary>
    public Task<ContributionInstance> GetContributionInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        string normalizedInstanceId = NormalizeContributionInstanceId(instanceId);
        if (normalizedInstanceId == null)
            return Task.FromResult<ContributionInstance>(null);

        return _mongoOperations.GetContributionInstanceAsync(normalizedInstanceId, cancellationToken);
    }

    /// <summary>
    /// Lists contribution instances optionally filtered by contribution definition.
    /// </summary>
    public Task<List<ContributionInstance>> GetContributionInstancesAsync(string contributionDefinitionId = null, CancellationToken cancellationToken = default)
    {
        string normalizedContributionDefinitionId = contributionDefinitionId == null ? null : NormalizeContributionId(contributionDefinitionId);
        return _mongoOperations.GetContributionInstancesAsync(normalizedContributionDefinitionId, cancellationToken);
    }

    /// <summary>
    /// Creates or updates one contribution instance when allowed by its definition.
    /// </summary>
    public async Task<ContributionInstance> UpsertContributionInstanceAsync(ContributionInstance instance, CancellationToken cancellationToken = default)
    {
        string normalizedContributionDefinitionId = NormalizeContributionId(instance?.ContributionDefinitionId);
        if (normalizedContributionDefinitionId == null || instance == null)
            return null;

        ContributionDefinition definition = await GetContributionDefinitionAsync(normalizedContributionDefinitionId, cancellationToken);
        if (definition == null || !definition.CanCreateInstances)
            return null;

        List<ContributionInstance> existingInstances = await _mongoOperations.GetContributionInstancesAsync(normalizedContributionDefinitionId, cancellationToken);
        string normalizedInstanceId = NormalizeContributionInstanceId(instance.Id) ?? Guid.NewGuid().ToString("N").ToLowerInvariant();
        ContributionInstance existingInstance = existingInstances.FirstOrDefault(existing => existing.Id == normalizedInstanceId);

        if (!definition.CanInstallMultipleTimes && existingInstance == null && existingInstances.Count > 0)
            return null;

        PrepareContributionInstanceForSave(instance, normalizedInstanceId, definition, existingInstance, DateTimeOffset.UtcNow);
        await _mongoOperations.SaveContributionInstanceAsync(instance, cancellationToken);
        return await _mongoOperations.GetContributionInstanceAsync(normalizedInstanceId, cancellationToken);
    }

    /// <summary>
    /// Requests one configuration step for a contribution instance through local interprocess messaging.
    /// </summary>
    public async Task<ContributionConfigurationResponse> ConfigureContributionInstanceAsync(string instanceId, Dictionary<string, string> setupValues = null, CancellationToken cancellationToken = default)
    {
        string normalizedInstanceId = NormalizeContributionInstanceId(instanceId);
        if (normalizedInstanceId == null)
            return null;

        ContributionInstance instance = await GetContributionInstanceAsync(normalizedInstanceId, cancellationToken);
        if (instance == null)
            return null;

        ContributionDefinition definition = await GetContributionDefinitionAsync(instance.ContributionDefinitionId, cancellationToken);
        if (definition == null)
            return null;

        ContributionConfigurationMessage message = new ContributionConfigurationMessage(definition.PluginId, definition, instance)
        {
            SetupValues = setupValues ?? []
        };

        ContributionConfigurationResponse response = NatsInterprocess.Request<ContributionConfigurationResponse>(message.Topic, message, 5000);
        if (response?.Instance == null)
            return null;

        PrepareConfiguredContributionInstanceForSave(response.Instance, definition, instance, DateTimeOffset.UtcNow);
        await _mongoOperations.SaveContributionInstanceAsync(response.Instance, cancellationToken);
        return response;
    }

    /// <summary>
    /// Deletes a contribution instance by identifier.
    /// </summary>
    public async Task<bool> DeleteContributionInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        string normalizedInstanceId = NormalizeContributionInstanceId(instanceId);
        if (normalizedInstanceId == null)
            return false;

        DeleteResult deleteResult = await _mongoOperations.DeleteContributionInstanceAsync(normalizedInstanceId, cancellationToken);
        return deleteResult.DeletedCount == 1;
    }

    internal static string NormalizeContributionInstanceId(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId))
            return null;

        return instanceId.Trim().ToLowerInvariant();
    }

    private static void PrepareContributionInstanceForSave(ContributionInstance instance, string normalizedInstanceId, ContributionDefinition definition, ContributionInstance existingInstance, DateTimeOffset now)
    {
        instance.Id = normalizedInstanceId;
        instance.ContributionDefinitionId = definition.Id;
        instance.PluginId = definition.PluginId;
        instance.Label = string.IsNullOrWhiteSpace(instance.Label)
            ? (string.IsNullOrWhiteSpace(existingInstance?.Label) ? definition.Label : existingInstance.Label)
            : instance.Label;
        instance.Settings ??= [];
        if (instance.Status == ContributionInstanceStatus.NotConfigured && instance.IsConfigured)
            instance.Status = ContributionInstanceStatus.Functional;

        instance.CreatedAtUtc = existingInstance?.CreatedAtUtc == default || existingInstance == null
            ? (instance.CreatedAtUtc == default ? now : instance.CreatedAtUtc)
            : existingInstance.CreatedAtUtc;
        instance.UpdatedAtUtc = now;
    }

    private static void PrepareConfiguredContributionInstanceForSave(ContributionInstance instance, ContributionDefinition definition, ContributionInstance existingInstance, DateTimeOffset now)
    {
        string normalizedInstanceId = NormalizeContributionInstanceId(instance.Id) ?? existingInstance.Id;
        PrepareContributionInstanceForSave(instance, normalizedInstanceId, definition, existingInstance, now);
    }
}