using System.Collections.Generic;

namespace MaNoir.Core.Contracts.Models.AdminUi;

public sealed class AdminNavigationDomainsResponse
{
    public AdminNavigationDomainsResponse()
    {
        Domains = [];
    }

    public List<AdminNavigationDomainSummary> Domains { get; set; }
}

public sealed class AdminNavigationDomainSummary
{
    public string Id { get; set; }

    public string Label { get; set; }

    public string Icon { get; set; }

    public string Href { get; set; }
}

public sealed class AdminDomainNavigationResponse
{
    public AdminNavigationDomainSummary Domain { get; set; }

    public List<AdminNavigationSection> Sections { get; set; } = [];
}

public sealed class AdminNavigationSection
{
    public string Id { get; set; }

    public string Label { get; set; }

    public List<AdminNavigationPage> Pages { get; set; } = [];
}

public sealed class AdminNavigationPage
{
    public string Id { get; set; }

    public string ContributionId { get; set; }

    public string PluginId { get; set; }

    public string Category { get; set; }

    public string Name { get; set; }

    public string Label { get; set; }

    public string Href { get; set; }
}