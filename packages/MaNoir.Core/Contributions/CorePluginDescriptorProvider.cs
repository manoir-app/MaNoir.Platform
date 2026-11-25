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
            RepositoryUrl = "https://github.com/manoir-app/MaNoir.Platform",
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
                                Category = "Overview",
                                Name = "Status",
                                Icon = "gauge",
                                Url = "/platform/mesh/status",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "General status",
                                    ["fr-FR"] = "Statut general"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "House",
                                Name = "Places",
                                Icon = "map-pin",
                                Url = "/platform/mesh/places",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Places and rooms",
                                    ["fr-FR"] = "Lieux et pieces"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "House",
                                Name = "Screens",
                                Icon = "monitor",
                                Url = "/platform/house/screens",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Linked screens",
                                    ["fr-FR"] = "Ecrans associes"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "House",
                                Name = "Local",
                                Icon = "sliders",
                                Url = "/platform/house/local",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Local settings",
                                    ["fr-FR"] = "Reglages locaux"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "People",
                                Name = "Users",
                                Icon = "users",
                                Url = "/platform/people/users",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Users and access",
                                    ["fr-FR"] = "Utilisateurs et droits"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Monitoring",
                                Name = "Services",
                                Icon = "server",
                                Url = "/platform/surveillance/services",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Services and agents",
                                    ["fr-FR"] = "Services et agents"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Monitoring",
                                Name = "Logs",
                                Icon = "file-text",
                                Url = "/platform/surveillance/logs",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Logs",
                                    ["fr-FR"] = "Journaux"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Extensions",
                                Name = "Catalog",
                                Icon = "puzzle",
                                Url = "/platform/extensions/catalog",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Plugins and contributions",
                                    ["fr-FR"] = "Plugins et contributions"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "Extensions",
                                Name = "Installed",
                                Icon = "package",
                                Url = "/platform/extensions/installed",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Installed plugins",
                                    ["fr-FR"] = "Plugins installes"
                                }
                            },
                            new AdminUiPageDefinition()
                            {
                                Category = "System",
                                Name = "About",
                                Icon = "server",
                                Url = "/platform/system/about",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Files, secrets and version",
                                    ["fr-FR"] = "Fichiers, secrets et version"
                                }
                            }
                        ]
                    }
                }
            ]
        };
    }
}