using Home.Common.Messages;
using MaNoir.Core.Contracts.Models.Contributions;
using MaNoir.Core.Secrets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Contributions;

public sealed partial class ContributionLogic
{
    private const string AuthorizationPendingStatusMessage = "Authorization required before the plugin can receive referenced secrets.";

    /// <summary>
    /// Marks one contribution instance as trusted to receive referenced secrets.
    /// </summary>
    public async Task<ContributionInstance> AuthorizeContributionInstanceAsync(string instanceId, CancellationToken cancellationToken = default)
    {
        string normalizedInstanceId = NormalizeContributionInstanceId(instanceId);
        if (normalizedInstanceId == null)
            return null;

        ContributionInstance instance = await GetContributionInstanceAsync(normalizedInstanceId, cancellationToken);
        if (instance == null || instance.Status == ContributionInstanceStatus.Archived)
            return null;

        ContributionDefinition definition = await GetContributionDefinitionAsync(instance.ContributionDefinitionId, cancellationToken);
        if (definition == null)
            return null;

        instance.AuthorizedAtUtc = DateTimeOffset.UtcNow;
        PrepareConfiguredContributionInstanceForSave(instance, definition, instance, DateTimeOffset.UtcNow);
        await _mongoOperations.SaveContributionInstanceAsync(instance, cancellationToken);
        return await _mongoOperations.GetContributionInstanceAsync(normalizedInstanceId, cancellationToken);
    }

    /// <summary>
    /// Resolves all secret references configured on one contribution instance and encrypts them for the supplied public key.
    /// </summary>
    public async Task<ContributionSecretsResponse> ResolveContributionInstanceSecretsAsync(string pluginId, string instanceId, string publicKeyPem, CancellationToken cancellationToken = default)
    {
        string normalizedPluginId = NormalizePluginId(pluginId);
        string normalizedInstanceId = NormalizeContributionInstanceId(instanceId);
        ContributionSecretsResponse response = new ContributionSecretsResponse()
        {
            InstanceId = normalizedInstanceId,
            Response = "error"
        };

        if (normalizedPluginId == null || normalizedInstanceId == null || string.IsNullOrWhiteSpace(publicKeyPem))
        {
            response.InstanceStatus = ContributionInstanceStatus.Error;
            response.InstanceStatusMessage = "The plugin identifier, instance identifier and public key are required.";
            return response;
        }

        ContributionInstance instance = await GetContributionInstanceAsync(normalizedInstanceId, cancellationToken);
        if (instance == null)
        {
            response.InstanceStatus = ContributionInstanceStatus.Error;
            response.InstanceStatusMessage = $"Contribution instance '{normalizedInstanceId}' was not found.";
            return response;
        }

        response.InstanceStatus = instance.Status;
        response.InstanceStatusMessage = instance.StatusMessage;

        if (!string.Equals(instance.PluginId, normalizedPluginId, StringComparison.OrdinalIgnoreCase))
        {
            response.InstanceStatus = ContributionInstanceStatus.Error;
            response.InstanceStatusMessage = $"Contribution instance '{normalizedInstanceId}' does not belong to plugin '{normalizedPluginId}'.";
            return response;
        }

        if (instance.Status == ContributionInstanceStatus.Archived)
        {
            response.Response = "archived";
            return response;
        }

        HashSet<string> referencedSecretIds = ContributionSecretReferenceHelper.GetReferencedSecretIds(instance.Settings);
        if (referencedSecretIds.Count == 0)
        {
            response.Response = "ok";
            return response;
        }

        if (instance.AuthorizedAtUtc == null)
        {
            response.Response = "authorization-pending";
            response.InstanceStatus = ContributionInstanceStatus.AuthorizationPending;
            response.InstanceStatusMessage = AuthorizationPendingStatusMessage;
            return response;
        }

        foreach (string secretId in referencedSecretIds.OrderBy(secretId => secretId, StringComparer.OrdinalIgnoreCase))
        {
            string clearText = await _sharedSecretLogic.GetSecretAsync(secretId, cancellationToken);
            if (clearText == null)
            {
                response.Response = "missing-secret";
                response.InstanceStatus = ContributionInstanceStatus.Error;
                response.InstanceStatusMessage = $"Shared secret '{secretId}' was not found.";
                response.Secrets.Clear();
                return response;
            }

            response.Secrets[secretId] = SharedSecretExchangeProtector.ProtectForPublicKey(clearText, publicKeyPem);
        }

        response.Response = "ok";
        response.InstanceStatus = instance.Status;
        response.InstanceStatusMessage = instance.StatusMessage;
        return response;
    }

    private static void ApplyContributionInstanceState(ContributionInstance instance, ContributionInstance existingInstance)
    {
        if (instance.Status == ContributionInstanceStatus.Archived || instance.Status == ContributionInstanceStatus.Error)
            return;

        bool hasSecretReferences = ContributionSecretReferenceHelper.ContainsSecretReferences(instance.Settings);
        if (hasSecretReferences && instance.AuthorizedAtUtc == null)
        {
            instance.Status = ContributionInstanceStatus.AuthorizationPending;
            if (string.IsNullOrWhiteSpace(instance.StatusMessage) || string.Equals(instance.StatusMessage, AuthorizationPendingStatusMessage, StringComparison.Ordinal))
                instance.StatusMessage = AuthorizationPendingStatusMessage;

            return;
        }

        if (!instance.IsConfigured)
        {
            instance.Status = instance.Settings.Count == 0
                ? ContributionInstanceStatus.NotConfigured
                : ContributionInstanceStatus.IncompleteConfiguration;

            if (string.Equals(instance.StatusMessage, AuthorizationPendingStatusMessage, StringComparison.Ordinal))
                instance.StatusMessage = null;

            return;
        }

        if (instance.Status == ContributionInstanceStatus.AuthorizationPending || instance.Status == ContributionInstanceStatus.NotConfigured || instance.Status == ContributionInstanceStatus.IncompleteConfiguration)
            instance.Status = ContributionInstanceStatus.Functional;

        if (string.Equals(instance.StatusMessage, AuthorizationPendingStatusMessage, StringComparison.Ordinal))
            instance.StatusMessage = existingInstance?.Status == ContributionInstanceStatus.Functional ? existingInstance.StatusMessage : null;
    }

    private static DateTimeOffset? ResolveAuthorizedAtUtc(ContributionInstance instance, ContributionInstance existingInstance)
    {
        HashSet<string> currentSecretIds = ContributionSecretReferenceHelper.GetReferencedSecretIds(instance.Settings);
        HashSet<string> existingSecretIds = existingInstance == null
            ? []
            : ContributionSecretReferenceHelper.GetReferencedSecretIds(existingInstance.Settings);

        if (!currentSecretIds.SetEquals(existingSecretIds))
            return null;

        return instance.AuthorizedAtUtc ?? existingInstance?.AuthorizedAtUtc;
    }
}