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
                        Domain = "Core",
                        AccessZoneId = CoreAccessZones.CoreAdminUi,
                        RequiredAccessLevel = AccessLevel.Read,
                        Pages =
                        [
                            new AdminUiPageDefinition()
                            {
                                Category = "General",
                                Name = "Home",
                                Url = "/admin/core",
                                Labels = new Dictionary<string, string>()
                                {
                                    ["en"] = "Core",
                                    ["fr-FR"] = "Noyau"
                                }
                            }
                        ]
                    }
                }
            ]
        };
    }
}