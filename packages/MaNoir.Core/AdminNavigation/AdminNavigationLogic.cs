using MaNoir.Core.Authorization;
using MaNoir.Core.Contributions;
using MaNoir.Core.Contracts.Models.AdminUi;
using MaNoir.Core.Contracts.Models.Contributions;
using System;
using System.Collections.Generic;
using System.Linq;
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
        string normalizedDomainId = ApplicationDomains.Resolve(domainId)?.Id;
        if (normalizedDomainId == null)
            return null;

        List<AdminDomainNavigationResponse> domains = await BuildDomainNavigationAsync(userId, cancellationToken);
        return domains.SingleOrDefault(domain => string.Equals(domain.Domain?.Id, normalizedDomainId, StringComparison.OrdinalIgnoreCase));
    }

    private static async Task<List<AdminDomainNavigationResponse>> BuildDomainNavigationAsync(string userId, CancellationToken cancellationToken)
    {
        ContributionLogic contributionLogic = new ContributionLogic();
        AuthorizationLogic authorizationLogic = new AuthorizationLogic();
        List<InstalledPlugin> plugins = await contributionLogic.GetInstalledPluginsAsync(cancellationToken);

        return await ComposeNavigationAsync(plugins, contribution => HasAccessAsync(userId, contribution, authorizationLogic, cancellationToken));
    }

    /// <summary>
    /// Composes the domain navigation from the installed plugins.
    /// A contribution is exposed in each of its domains when its required plugins are available and the access check succeeds.
    /// </summary>
    internal static async Task<List<AdminDomainNavigationResponse>> ComposeNavigationAsync(
        IReadOnlyCollection<InstalledPlugin> plugins,
        Func<ContributionDefinition, Task<bool>> hasAccessAsync)
    {
        HashSet<string> availablePluginIds = new(
            plugins.Where(plugin => plugin?.Id != null && plugin.IsEnabled && plugin.IsHealthy).Select(plugin => plugin.Id),
            StringComparer.OrdinalIgnoreCase);
        Dictionary<string, DomainBuilder> domainsById = new(StringComparer.OrdinalIgnoreCase);

        foreach (InstalledPlugin plugin in plugins.Where(plugin => plugin?.Contributions != null))
        {
            IEnumerable<ContributionDefinition> adminUiContributions = plugin.Contributions
                .Where(contribution => contribution?.Kind == ContributionKind.AdminUiPage && contribution.AdminUi != null)
                .OrderBy(contribution => contribution.Id, StringComparer.OrdinalIgnoreCase);

            foreach (ContributionDefinition contribution in adminUiContributions)
            {
                if (!AreRequirementsMet(contribution, availablePluginIds) || !await hasAccessAsync(contribution))
                    continue;

                foreach (ApplicationDomain applicationDomain in ApplicationDomains.ResolveAll(contribution.AdminUi.Domains))
                {
                    if (!domainsById.TryGetValue(applicationDomain.Id, out DomainBuilder domain))
                    {
                        domain = new DomainBuilder(applicationDomain);
                        domainsById.Add(applicationDomain.Id, domain);
                    }

                    foreach (AdminUiPageDefinition page in contribution.AdminUi.Pages ?? [])
                    {
                        (string href, bool isHostedByModule) = ResolvePageTarget(page, plugin.AdminUiPathPrefix);
                        if (string.IsNullOrWhiteSpace(href))
                            continue;

                        domain.AddPage(plugin, contribution, page, href, isHostedByModule);
                    }
                }
            }
        }

        return [.. domainsById.Values
            .Where(domain => domain.HasPages)
            .OrderBy(domain => domain.Order)
            .ThenBy(domain => domain.Label, StringComparer.CurrentCultureIgnoreCase)
            .Select(domain => domain.Build())];
    }

    private static bool AreRequirementsMet(ContributionDefinition contribution, HashSet<string> availablePluginIds)
    {
        return (contribution.Requires ?? [])
            .Where(pluginId => !string.IsNullOrWhiteSpace(pluginId))
            .All(pluginId => availablePluginIds.Contains(pluginId.Trim()));
    }

    private static async Task<bool> HasAccessAsync(string userId, ContributionDefinition contribution, AuthorizationLogic authorizationLogic, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(contribution.AdminUi.AccessZoneId))
            return true;

        if (string.IsNullOrWhiteSpace(userId))
            return false;

        return await authorizationLogic.HasAccessAsync(userId, contribution.AdminUi.AccessZoneId, contribution.AdminUi.RequiredAccessLevel, cancellationToken);
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

    /// <summary>
    /// Resolves where a page lives. A relative path belongs to the plugin's own Admin UI: once that one is deployed
    /// under a base path, it is another application than the Core Admin UI.
    /// </summary>
    private static (string Href, bool IsHostedByModule) ResolvePageTarget(AdminUiPageDefinition page, string adminUiPathPrefix)
    {
        if (!string.IsNullOrWhiteSpace(page?.RelativePath))
        {
            string relativePath = page.RelativePath.Trim();
            if (!relativePath.StartsWith("/", StringComparison.Ordinal))
                relativePath = "/" + relativePath;

            return (AdminUiPublicPath.Compose(adminUiPathPrefix, relativePath), AdminUiPublicPath.Normalize(adminUiPathPrefix) != null);
        }

        return (page?.Url, false);
    }

    private static string TryResolveLabel(IReadOnlyDictionary<string, string> labels, string culture)
    {
        KeyValuePair<string, string> match = labels.FirstOrDefault(entry => string.Equals(entry.Key, culture, StringComparison.OrdinalIgnoreCase));
        return string.IsNullOrWhiteSpace(match.Value) ? null : match.Value;
    }

    private sealed class DomainBuilder
    {
        private readonly Dictionary<string, SectionBuilder> _sectionsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<SectionBuilder> _sections = [];

        public DomainBuilder(ApplicationDomain domain)
        {
            Id = domain.Id;
            Label = domain.Label;
            Icon = domain.Icon;
            Order = domain.Order;
        }

        public string Id { get; }

        public string Label { get; }

        public string Icon { get; }

        public int Order { get; }

        public bool HasPages => _sections.Any(section => section.Pages.Count > 0);

        public void AddPage(InstalledPlugin plugin, ContributionDefinition contribution, AdminUiPageDefinition page, string href, bool isHostedByModule)
        {
            string category = string.IsNullOrWhiteSpace(page.Category) ? "General" : page.Category.Trim();
            string sectionId = ApplicationDomains.Slugify(category) ?? "general";
            if (!_sectionsById.TryGetValue(sectionId, out SectionBuilder section))
            {
                section = new SectionBuilder(sectionId, category);
                _sectionsById.Add(sectionId, section);
                _sections.Add(section);
            }

            section.Pages.Add(new AdminNavigationPage()
            {
                Id = string.Concat(contribution.Id, ":", ApplicationDomains.Slugify(page.Name) ?? "page"),
                ContributionId = contribution.Id,
                PluginId = plugin.Id,
                Category = category,
                Name = page.Name,
                Icon = page.Icon,
                Label = ResolvePageLabel(page),
                Href = href,
                IsHostedByModule = isHostedByModule
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

            // Opening a domain stays in the Core Admin UI whenever it has a page there.
            AdminNavigationPage landingPage = sections
                .SelectMany(section => section.Pages)
                .OrderBy(page => page.IsHostedByModule)
                .FirstOrDefault();

            return new AdminDomainNavigationResponse()
            {
                Domain = new AdminNavigationDomainSummary()
                {
                    Id = Id,
                    Label = Label,
                    Icon = Icon,
                    Href = landingPage?.Href,
                    IsHostedByModule = landingPage?.IsHostedByModule ?? false
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