using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Contributions;
using System.Collections.Generic;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Builds the published Core plugin descriptor.
/// </summary>
public static class CorePluginDescriptorProvider
{
    /// <summary>
    /// Creates the current Core plugin descriptor.
    /// </summary>
    public static PluginDescriptor Create(string version)
    {
        return new PluginDescriptor()
        {
            Id = "core",
            Label = "Core",
            Version = string.IsNullOrWhiteSpace(version) ? "0.0.0" : version,
            Description = "Core platform capabilities.",
            Publisher = "MaNoir",
            AccessZones = [.. CoreAccessZones.GetDefinitions()],
            Contributions =
            [
                new ContributionDefinition()
                {
                    Id = "core.admin.pages",
                    Kind = ContributionKind.AdminUiPage,
                    Label = "Core admin",
                    Description = "Core administration pages.",
                    CanCreateInstances = false,
                    AdminUi = new AdminUiContributionDefinitionData()
                    {
                        Domain = "Platform",
                        AccessZoneId = CoreAccessZones.CoreAdminUi,
                        RequiredAccessLevel = AccessLevel.Read,
                        Pages =
                        [
                            new AdminUiPageDefinition()
                            {
                                Category = "Mesh",
                                Name = "Status",
                                Url = "/platform/mesh/status",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "General status",
                                    ["fr-FR"] = "Statut general"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Mesh",
                                Name = "Places",
                                Url = "/platform/mesh/places",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Places and rooms",
                                    ["fr-FR"] = "Lieux et pieces"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Surveillance",
                                Name = "Agents",
                                Url = "/platform/surveillance/agents",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Agents",
                                    ["fr-FR"] = "Agents"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Surveillance",
                                Name = "Services",
                                Url = "/platform/surveillance/services",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Services",
                                    ["fr-FR"] = "Services"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Extensions",
                                Name = "Catalog",
                                Url = "/platform/extensions/catalog",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Plugins and contributions",
                                    ["fr-FR"] = "Plugins et contributions"
                                }
                            }
                        ]
                    }
                }
            ]
        };
    }
}