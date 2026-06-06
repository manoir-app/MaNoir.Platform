using MaNoir.Core.Authorization;
using MaNoir.Core.Contributions;
using MaNoir.Core.Contracts.Models.AdminUi;
using MaNoir.Core.Contracts.Models.Contributions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.AdminNavigation;

/// <summary>
/// Builds the admin navigation model exposed to admin clients.
/// </summary>
public sealed class AdminNavigationLogic
{
    /// <summary>
    /// Lists the domains visible to one authenticated user.
    /// </summary>
    public async Task<AdminNavigationDomainsResponse> GetDomainsAsync(string userId, CancellationToken cancellationToken = default)
    {
        List<AdminDomainNavigationResponse> domains = await BuildDomainNavigationAsync(userId, cancellationToken);
        return new AdminNavigationDomainsResponse()
        {
            Domains = [.. domains.Select(domain => domain.Domain)]
        };
    }

    /// <summary>
    /// Gets the sidebar navigation for one visible domain.
    /// </summary>
    public async Task<AdminDomainNavigationResponse> GetDomainAsync(string userId, string domainId, CancellationToken cancellationToken = default)
    {
        string normalizedDomainId = NormalizeDomainId(domainId);
        if (normalizedDomainId == null)
            return null;

        List<AdminDomainNavigationResponse> domains = await BuildDomainNavigationAsync(userId, cancellationToken);
        return domains.SingleOrDefault(domain => string.Equals(domain.Domain?.Id, normalizedDomainId, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<AdminDomainNavigationResponse>> BuildDomainNavigationAsync(string userId, CancellationToken cancellationToken)
    {
        ContributionLogic contributionLogic = new ContributionLogic();
        AuthorizationLogic authorizationLogic = new AuthorizationLogic();
        List<InstalledPlugin> plugins = await contributionLogic.GetInstalledPluginsByContributionKindAsync(ContributionKind.AdminUiPage, cancellationToken);
        Dictionary<string, DomainBuilder> domainsById = new(StringComparer.OrdinalIgnoreCase);

        foreach (InstalledPlugin plugin in plugins)
        {
            foreach (ContributionDefinition contribution in plugin.Contributions.Where(contribution => contribution?.AdminUi != null))
            {
                if (!await CanExposeContributionAsync(userId, contribution, authorizationLogic, cancellationToken))
                    continue;

                AdminDomainMetadata metadata = ResolveDomainMetadata(contribution.AdminUi.Domain);
                if (!domainsById.TryGetValue(metadata.Id, out DomainBuilder domain))
                {
                    domain = new DomainBuilder(metadata);
                    domainsById.Add(metadata.Id, domain);
                }

                foreach (AdminUiPageDefinition page in contribution.AdminUi.Pages ?? [])
                {
                    string href = ResolvePageHref(page);
                    if (page == null || string.IsNullOrWhiteSpace(href))
                        continue;

                    domain.AddPage(plugin, contribution, page, href);
                }
            }
        }

        return [.. domainsById.Values
            .Where(domain => domain.HasPages)
            .OrderBy(domain => domain.Order)
            .ThenBy(domain => domain.Label, StringComparer.CurrentCultureIgnoreCase)
            .Select(domain => domain.Build())];
    }

    private static async Task<bool> CanExposeContributionAsync(string userId, ContributionDefinition contribution, AuthorizationLogic authorizationLogic, CancellationToken cancellationToken)
    {
        if (contribution?.AdminUi == null)
            return false;

        if (string.IsNullOrWhiteSpace(contribution.AdminUi.AccessZoneId))
            return true;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        return await authorizationLogic.HasAccessAsync(userId, contribution.AdminUi.AccessZoneId, contribution.AdminUi.RequiredAccessLevel, cancellationToken);
    }

    private static AdminDomainMetadata ResolveDomainMetadata(string domain)
    {
        string normalizedDomainId = NormalizeDomainId(domain);

        return normalizedDomainId switch
        {
            "platform" => new AdminDomainMetadata("platform", "Platform", "platform", 0),
            "home-automation" => new AdminDomainMetadata("home-automation", "Home Automation", "home-automation", 100),
            "daily-life" => new AdminDomainMetadata("daily-life", "Vie quotidienne", "daily-life", 200),
            _ => new AdminDomainMetadata(normalizedDomainId ?? "general", string.IsNullOrWhiteSpace(domain) ? "General" : domain.Trim(), "generic", 500)
        };
    }

    private static string NormalizeDomainId(string domain)
    {
        if (string.IsNullOrWhiteSpace(domain))
            return null;

        string trimmedDomain = domain.Trim();
        string alias = trimmedDomain.ToLowerInvariant() switch
        {
            "core" => "platform",
            "general" => "platform",
            _ => trimmedDomain
        };

        StringBuilder builder = new();
        bool previousWasSeparator = false;

        foreach (char character in alias.Normalize(NormalizationForm.FormD))
        {
            UnicodeCategory category = CharUnicodeInfo.GetUnicodeCategory(character);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;

            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSeparator = false;
                continue;
            }

            if (!previousWasSeparator && builder.Length > 0)
            {
                builder.Append('-');
                previousWasSeparator = true;
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string ResolvePageLabel(AdminUiPageDefinition page)
    {
        if (page?.Labels == null || page.Labels.Count == 0)
            return page?.Name;

        return TryResolveLabel(page.Labels, "fr-FR")
            ?? TryResolveLabel(page.Labels, "fr")
            ?? TryResolveLabel(page.Labels, "en-US")
            ?? TryResolveLabel(page.Labels, "en")
            ?? page.Labels.OrderBy(entry => entry.Key, StringComparer.OrdinalIgnoreCase).Select(entry => entry.Value).FirstOrDefault()
            ?? page.Name;
    }

    private static string ResolvePageHref(AdminUiPageDefinition page)
    {
        if (!string.IsNullOrWhiteSpace(page?.RelativePath))
        {
            string relativePath = page.RelativePath.Trim();
            return relativePath.StartsWith("/", StringComparison.Ordinal) ? relativePath : "/" + relativePath;
        }

        return string.IsNullOrWhiteSpace(page?.Url) ? null : page.Url;
    }

    private static string TryResolveLabel(IReadOnlyDictionary<string, string> labels, string culture)
    {
        KeyValuePair<string, string> match = labels.FirstOrDefault(entry => string.Equals(entry.Key, culture, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match.Value) ? null : match.Value;
    }

    private sealed record AdminDomainMetadata(string Id, string Label, string Icon, int Order);

    private sealed class DomainBuilder
    {
        private readonly Dictionary<string, SectionBuilder> _sectionsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<SectionBuilder> _sections = [];

        public DomainBuilder(AdminDomainMetadata metadata)
        {
            Id = metadata.Id;
            Label = metadata.Label;
            Icon = metadata.Icon;
            Order = metadata.Order;
        }

        public string Id { get; }

        public string Label { get; }

        public string Icon { get; }

        public int Order { get; }

        public bool HasPages => _sections.Any(section => section.Pages.Count > 0);

        public void AddPage(InstalledPlugin plugin, ContributionDefinition contribution, AdminUiPageDefinition page, string href)
        {
            string category = string.IsNullOrWhiteSpace(page.Category) ? "General" : page.Category.Trim();
            string sectionId = NormalizeDomainId(category) ?? "general";
            if (!_sectionsById.TryGetValue(sectionId, out SectionBuilder section))
            {
                section = new SectionBuilder(sectionId, category);
                _sectionsById.Add(sectionId, section);
                _sections.Add(section);
            }

            section.Pages.Add(new AdminNavigationPage()
            {
                Id = string.Concat(contribution.Id, ":", NormalizeDomainId(page.Name) ?? "page"),
                ContributionId = contribution.Id,
                PluginId = plugin.Id,
                Category = category,
                Name = page.Name,
                Label = ResolvePageLabel(page),
                Href = href
            });
        }

        public AdminDomainNavigationResponse Build()
        {
            List<AdminNavigationSection> sections = [.. _sections
                .Select(section => new AdminNavigationSection()
                {
                    Id = section.Id,
                    Label = section.Label,
                    Pages = [.. section.Pages]
                })];

            string href = sections
                .SelectMany(section => section.Pages)
                .Select(page => page.Href)
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            return new AdminDomainNavigationResponse()
            {
                Domain = new AdminNavigationDomainSummary()
                {
                    Id = Id,
                    Label = Label,
                    Icon = Icon,
                    Href = href
                },
                Sections = sections
            };
        }
    }

    private sealed class SectionBuilder
    {
        public SectionBuilder(string id, string label)
        {
            Id = id;
            Label = label;
            Pages = [];
        }

        public string Id { get; }

        public string Label { get; }

        public List<AdminNavigationPage> Pages { get; }
    }
}