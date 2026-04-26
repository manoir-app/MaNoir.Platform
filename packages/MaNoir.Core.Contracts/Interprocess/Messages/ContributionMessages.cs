using MaNoir.Core.Contracts.Models.Contributions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace Home.Common.Messages
{
    public class PluginCatalogPublicationMessage : BaseMessage
    {
        public const string PublishTopic = "system.plugin.catalog.publish";

        public PluginCatalogPublicationMessage() : base(PublishTopic)
        {
            Plugin = new InstalledPlugin();
            Contributions = [];
        }

        public InstalledPlugin Plugin { get; set; }

        public List<ContributionDefinition> Contributions { get; set; }
    }

    public class ContributionDefinitionsChangedMessage : BaseMessage
    {
        public const string TopicName = "system.contribution.definitions.changed";

        public ContributionDefinitionsChangedMessage() : base(TopicName)
        {
        }

        public string PluginId { get; set; }
    }

    public class ContributionInstancesChangedMessage : BaseMessage
    {
        public const string TopicName = "system.contribution.instances.changed";

        public ContributionInstancesChangedMessage() : base(TopicName)
        {
        }

        public string PluginId { get; set; }

        public string ContributionDefinitionId { get; set; }
    }

    public class ContributionConfigurationMessage : BaseMessage
    {
        private static readonly string TopicFormat = "{0}.contribution.configure";

        public ContributionConfigurationMessage() : base(string.Empty)
        {
            SetupValues = [];
        }

        public ContributionConfigurationMessage(string pluginId, ContributionDefinition contribution, ContributionInstance instance) : this()
        {
            PluginId = pluginId;
            Contribution = contribution;
            Instance = instance;
        }

        private string _pluginId;

        public string PluginId
        {
            get { return _pluginId; }
            set
            {
                _pluginId = value;
                Topic = string.IsNullOrWhiteSpace(value) ? string.Empty : string.Format(TopicFormat, value.ToLowerInvariant());
            }
        }

        public ContributionDefinition Contribution { get; set; }

        public ContributionInstance Instance { get; set; }

        public Dictionary<string, string> SetupValues { get; set; }
    }

    public class ContributionConfigurationResponse : MessageResponse
    {
        public ContributionConfigurationResponse()
        {
            Fields = [];
        }

        public ContributionConfigurationResponse(ContributionConfigurationMessage source) : this()
        {
            if (source?.Instance != null)
            {
                Instance = JsonConvert.DeserializeObject<ContributionInstance>(JsonConvert.SerializeObject(source.Instance));
            }
            else
            {
                Instance = new ContributionInstance()
                {
                    Id = Guid.NewGuid().ToString("N"),
                    ContributionDefinitionId = source?.Contribution?.Id,
                    PluginId = source?.PluginId,
                    Label = source?.Contribution?.Label,
                    Settings = []
                };
            }
        }

        public ContributionInstance Instance { get; set; }

        public List<ContributionConfigurationField> Fields { get; set; }

        public bool IsFinalStep { get; set; }
    }

    public class ContributionConfigurationField
    {
        public ContributionConfigurationField()
        {
            Options = [];
        }

        public string Id { get; set; }

        public string Label { get; set; }

        public string Type { get; set; }

        public bool IsRequired { get; set; }

        public string CurrentValue { get; set; }

        public List<ContributionConfigurationFieldOption> Options { get; set; }
    }

    public class ContributionConfigurationFieldOption
    {
        public string Value { get; set; }

        public string Label { get; set; }
    }

    public class ContributionSecretsRequestMessage : BaseMessage
    {
        private static readonly string TopicFormat = "{0}.contribution.secrets.resolve";

        public ContributionSecretsRequestMessage() : base(string.Empty)
        {
        }

        public ContributionSecretsRequestMessage(string pluginId, string instanceId, string publicKeyPem) : this()
        {
            PluginId = pluginId;
            InstanceId = instanceId;
            PublicKeyPem = publicKeyPem;
        }

        private string _pluginId;

        public string PluginId
        {
            get { return _pluginId; }
            set
            {
                _pluginId = value;
                Topic = string.IsNullOrWhiteSpace(value) ? string.Empty : string.Format(TopicFormat, value.ToLowerInvariant());
            }
        }

        public string InstanceId { get; set; }

        public string PublicKeyPem { get; set; }
    }

    public class ContributionSecretsResponse : MessageResponse
    {
        public ContributionSecretsResponse()
        {
            Secrets = [];
        }

        public string InstanceId { get; set; }

        public ContributionInstanceStatus InstanceStatus { get; set; }

        public string InstanceStatusMessage { get; set; }

        public Dictionary<string, ContributionEncryptedSecretPayload> Secrets { get; set; }
    }

    public class ContributionEncryptedSecretPayload
    {
        public string EncryptionMode { get; set; }

        public string EncryptedKey { get; set; }

        public string EncryptedData { get; set; }

        public string Nonce { get; set; }

        public string AuthenticationTag { get; set; }
    }
}