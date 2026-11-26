using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MaNoir.Core.AdminNavigation;

/// <summary>
/// Describes one application domain, meaning a place where the user finds features.
/// </summary>
/// <param name="Id">The canonical domain identifier.</param>
/// <param name="Label">The display label.</param>
/// <param name="Icon">The icon name known by the Admin UI.</param>
/// <param name="Order">The position in the navigation; lower values come first.</param>
public sealed record ApplicationDomain(string Id, string Label, string Icon, int Order);

/// <summary>
/// Declares the application domains and resolves the domain names published by plugin descriptors.
/// </summary>
/// <remarks>
/// Application domains describe where features are used. They are unrelated to the groups of the plugin catalog,
/// which only organize plugin publication.
/// </remarks>
public static class ApplicationDomains
{
    private const string GenericIcon = "generic";
    private const int UnknownDomainOrder = 500;

    /// <summary>The "today" domain: what matters right now.</summary>
    public static ApplicationDomain Today { get; } = new("today", "Aujourd'hui", "today", 0);

    /// <summary>The kitchen domain.</summary>
    public static ApplicationDomain Kitchen { get; } = new("kitchen", "Cuisine", "kitchen", 100);

    /// <summary>The home domain.</summary>
    public static ApplicationDomain Home { get; } = new("home", "Maison", "home", 200);

    /// <summary>The projects and hobbies domain.</summary>
    public static ApplicationDomain Projects { get; } = new("projects", "Projets et loisirs", "projects", 300);

    /// <summary>The settings domain, always listed last.</summary>
    public static ApplicationDomain Settings { get; } = new("settings", "Réglages", "settings", 1000);

    /// <summary>
    /// Gets the declared application domains, in navigation order.
    /// </summary>
    public static IReadOnlyList<ApplicationDomain> All { get; } = [Today, Kitchen, Home, Projects, Settings];

    private static readonly Dictionary<string, ApplicationDomain> DeclaredDomainsById = All.ToDictionary(domain => domain.Id, StringComparer.Ordinal);

    // Keys are slugs. The unseparated forms cover names written as one word, such as "HomeAutomation".
    private static readonly Dictionary<string, ApplicationDomain> Aliases = new(StringComparer.Ordinal)
    {
        ["platform"] = Settings,
        ["core"] = Settings,
        ["general"] = Settings,
        ["home-automation"] = Home,
        ["homeautomation"] = Home,
        ["inventory"] = Home,
        ["daily-life"] = Today,
        ["dailylife"] = Today
    };

    /// <summary>
    /// Resolves a domain name to a declared domain, following aliases.
    /// A name that matches nothing yields a generic domain, and a blank name yields <see langword="null"/>.
    /// </summary>
    public static ApplicationDomain Resolve(string domain)
    {
        string slug = Slugify(domain);
        if (slug == null)
            return null;

        if (DeclaredDomainsById.TryGetValue(slug, out ApplicationDomain declaredDomain))
            return declaredDomain;

        if (Aliases.TryGetValue(slug, out ApplicationDomain aliasedDomain))
            return aliasedDomain;

        return new ApplicationDomain(slug, domain.Trim(), GenericIcon, UnknownDomainOrder);
    }

    /// <summary>
    /// Resolves the domains targeted by one contribution, without duplicates.
    /// A contribution that names no domain belongs to <see cref="Settings"/>.
    /// </summary>
    public static IReadOnlyList<ApplicationDomain> ResolveAll(IEnumerable<string> domains)
    {
        List<ApplicationDomain> resolvedDomains = [.. (domains ?? [])
            .Select(Resolve)
            .Where(domain => domain != null)
            .DistinctBy(domain => domain.Id, StringComparer.Ordinal)];

        return resolvedDomains.Count == 0 ? [Settings] : resolvedDomains;
    }

    /// <summary>
    /// Turns a display name into a lowercase identifier without accents, using dashes as separators.
    /// Returns <see langword="null"/> when nothing usable remains.
    /// </summary>
    internal static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        StringBuilder builder = new();
        bool previousWasSeparator = false;

        foreach (char character in value.Trim().Normalize(NormalizationForm.FormD))
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

        string slug = builder.ToString().Trim('-');
        return slug.Length == 0 ? null : slug;
    }
}
