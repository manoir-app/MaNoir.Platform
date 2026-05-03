using MaNoir.Core.Contracts.Models.Mesh;
using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;

namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
    /// <summary>
    /// Normalizes a frontend code for comparisons and persistence.
    /// </summary>
    /// <param name="frontendCode">The raw frontend code.</param>
    /// <returns>The normalized lower-case code, or <see langword="null"/> when missing.</returns>
    public static string NormalizeFrontendCode(string frontendCode)
    {
        if (string.IsNullOrWhiteSpace(frontendCode))
            return null;

        return frontendCode.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Normalizes a frontend URL before persistence.
    /// </summary>
    /// <param name="frontendUrl">The raw frontend URL.</param>
    /// <returns>The normalized absolute URL, or <see langword="null"/> when invalid.</returns>
    public static string NormalizeFrontendUrl(string frontendUrl)
    {
        if (string.IsNullOrWhiteSpace(frontendUrl))
            return null;

        if (!Uri.TryCreate(frontendUrl.Trim(), UriKind.Absolute, out Uri normalizedUri))
            return null;

        return normalizedUri.AbsoluteUri;
    }

    /// <summary>
    /// Gets a frontend URL from the mesh catalog.
    /// </summary>
    /// <param name="mesh">The mesh to inspect.</param>
    /// <param name="frontendCode">The frontend code to find.</param>
    /// <returns>The matching frontend URL, or <see langword="null"/> when missing.</returns>
    public static string GetFrontendUrl(AutomationMesh mesh, string frontendCode)
    {
        if (mesh == null)
            return null;

        string normalizedFrontendCode = NormalizeFrontendCode(frontendCode);
        if (normalizedFrontendCode == null)
            return null;

        EnsureFrontendUrls(mesh);
        return mesh.FrontendUrls.TryGetValue(normalizedFrontendCode, out string frontendUrl) ? frontendUrl : null;
    }

    /// <summary>
    /// Gets the frontend URL catalog from the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to inspect.</param>
    /// <returns>A stable copy of the frontend URLs indexed by code.</returns>
    public static Dictionary<string, string> GetFrontendUrls(AutomationMesh mesh)
    {
        if (mesh == null)
            return [];

        EnsureFrontendUrls(mesh);
        return new Dictionary<string, string>(mesh.FrontendUrls, StringComparer.InvariantCultureIgnoreCase);
    }

    /// <summary>
    /// Creates or updates one frontend URL on the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="frontendCode">The stable frontend code.</param>
    /// <param name="frontendUrl">The absolute frontend URL.</param>
    /// <returns><see langword="true"/> when the catalog changed.</returns>
    public static bool UpsertFrontendUrl(AutomationMesh mesh, string frontendCode, string frontendUrl)
    {
        if (mesh == null)
            return false;

        string normalizedFrontendCode = NormalizeFrontendCode(frontendCode);
        string normalizedFrontendUrl = NormalizeFrontendUrl(frontendUrl);
        if (normalizedFrontendCode == null || normalizedFrontendUrl == null)
            return false;

        EnsureFrontendUrls(mesh);
        if (mesh.FrontendUrls.TryGetValue(normalizedFrontendCode, out string existingFrontendUrl)
            && string.Equals(existingFrontendUrl, normalizedFrontendUrl, StringComparison.InvariantCulture))
        {
            return false;
        }

        mesh.FrontendUrls[normalizedFrontendCode] = normalizedFrontendUrl;
        return true;
    }

    /// <summary>
    /// Deletes one frontend URL from the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="frontendCode">The stable frontend code.</param>
    /// <returns><see langword="true"/> when the catalog changed.</returns>
    public static bool DeleteFrontendUrl(AutomationMesh mesh, string frontendCode)
    {
        if (mesh == null)
            return false;

        string normalizedFrontendCode = NormalizeFrontendCode(frontendCode);
        if (normalizedFrontendCode == null)
            return false;

        EnsureFrontendUrls(mesh);
        return mesh.FrontendUrls.Remove(normalizedFrontendCode);
    }

    /// <summary>
    /// Normalizes a global scenario code for comparisons and persistence.
    /// </summary>
    /// <param name="scenarioCode">The raw scenario code.</param>
    /// <returns>The normalized lower-case code, or <see langword="null"/> when missing.</returns>
    public static string NormalizeGlobalScenarioCode(string scenarioCode)
    {
        if (string.IsNullOrWhiteSpace(scenarioCode))
            return null;

        return scenarioCode.ToLowerInvariant();
    }

    /// <summary>
    /// Gets a global scenario by code.
    /// </summary>
    /// <param name="mesh">The mesh to inspect.</param>
    /// <param name="scenarioCode">The scenario code to find.</param>
    /// <returns>The matching scenario, or <see langword="null"/> when missing.</returns>
    public static AutomationMeshGlobalScenario GetGlobalScenario(AutomationMesh mesh, string scenarioCode)
    {
        if (mesh == null)
            return null;

        string normalizedScenarioCode = NormalizeGlobalScenarioCode(scenarioCode);
        if (normalizedScenarioCode == null)
            return null;

        return mesh.Scenarios.FirstOrDefault(item =>
            string.Equals(item.Code, normalizedScenarioCode, StringComparison.InvariantCultureIgnoreCase));
    }

    /// <summary>
    /// Creates or updates a global scenario on the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="scenario">The incoming scenario payload.</param>
    /// <returns>The upserted scenario, or <see langword="null"/> when the input is incomplete.</returns>
    public static AutomationMeshGlobalScenario UpsertGlobalScenario(AutomationMesh mesh, AutomationMeshGlobalScenario scenario)
    {
        if (mesh == null || scenario == null)
            return null;

        string normalizedScenarioCode = NormalizeGlobalScenarioCode(scenario.Code);
        if (normalizedScenarioCode == null)
            return null;

        if (mesh.Scenarios == null)
            mesh.Scenarios = [];

        AutomationMeshGlobalScenario existingScenario = GetGlobalScenario(mesh, normalizedScenarioCode);
        scenario.Code = normalizedScenarioCode;

        if (existingScenario != null)
        {
            if ((scenario.Images == null || scenario.Images.Count == 0) && existingScenario.Images != null && existingScenario.Images.Count > 0)
                scenario.Images = new Dictionary<string, string>(existingScenario.Images, StringComparer.InvariantCultureIgnoreCase);

            if (AreEquivalentGlobalScenarios(existingScenario, scenario))
                return existingScenario;

            mesh.Scenarios.Remove(existingScenario);
        }

        if (scenario.Images == null)
            scenario.Images = [];

        mesh.Scenarios.Add(scenario);
        return scenario;
    }

    private static bool AreEquivalentGlobalScenarios(AutomationMeshGlobalScenario left, AutomationMeshGlobalScenario right)
    {
        if (ReferenceEquals(left, right))
            return true;

        if (left == null || right == null)
            return false;

        if (!string.Equals(NormalizeGlobalScenarioCode(left.Code), NormalizeGlobalScenarioCode(right.Code), StringComparison.InvariantCulture))
            return false;

        if (!string.Equals(left.Label, right.Label, StringComparison.InvariantCulture))
            return false;

        if (!AreEquivalentScenarioImages(left.Images, right.Images))
            return false;

        return true;
    }

    private static bool AreEquivalentScenarioImages(Dictionary<string, string> left, Dictionary<string, string> right)
    {
        int leftCount = left == null ? 0 : left.Count;
        int rightCount = right == null ? 0 : right.Count;

        if (leftCount != rightCount)
            return false;

        if (leftCount == 0)
            return true;

        foreach (KeyValuePair<string, string> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out string value))
                return false;

            if (!string.Equals(entry.Value, value, StringComparison.InvariantCulture))
                return false;
        }

        return true;
    }

    private static void EnsureFrontendUrls(AutomationMesh mesh)
    {
        if (mesh.FrontendUrls == null)
            mesh.FrontendUrls = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

        string mainFrontendUrl = mesh.MainServer?.MainRole?.Uri;
        string normalizedMainFrontendUrl = NormalizeFrontendUrl(mainFrontendUrl);
        if (normalizedMainFrontendUrl != null && !mesh.FrontendUrls.ContainsKey("home"))
            mesh.FrontendUrls["home"] = normalizedMainFrontendUrl;
    }

    /// <summary>
    /// Deletes a global scenario from the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="scenarioCode">The scenario code to delete.</param>
    /// <returns><see langword="true"/> when a scenario was removed.</returns>
    public static bool DeleteGlobalScenario(AutomationMesh mesh, string scenarioCode)
    {
        if (mesh == null)
            return false;

        if (mesh.Scenarios == null)
            mesh.Scenarios = [];

        AutomationMeshGlobalScenario scenario = GetGlobalScenario(mesh, scenarioCode);
        if (scenario == null)
            return false;

        mesh.Scenarios.Remove(scenario);
        return true;
    }

    /// <summary>
    /// Sets the current global scenario on the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="scenarioCode">The scenario code to activate.</param>
    /// <returns><see langword="true"/> when the current scenario changed.</returns>
    public static bool SetCurrentGlobalScenario(AutomationMesh mesh, string scenarioCode)
    {
        if (mesh == null)
            return false;

        AutomationMeshGlobalScenario scenario = GetGlobalScenario(mesh, scenarioCode);
        if (scenario == null)
            return false;

        if (string.Equals(mesh.CurrentScenario, scenario.Code, StringComparison.InvariantCulture))
            return false;

        mesh.CurrentScenario = scenario.Code;
        return true;
    }

    /// <summary>
    /// Clears the current global scenario on the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <returns><see langword="true"/> when the current scenario changed.</returns>
    public static bool ClearCurrentGlobalScenario(AutomationMesh mesh)
    {
        if (mesh == null || mesh.CurrentScenario == null)
            return false;

        mesh.CurrentScenario = null;
        return true;
    }

    /// <summary>
    /// Applies a privacy mode to the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="privacyMode">The privacy mode to apply.</param>
    /// <returns><see langword="true"/> when the privacy mode changed.</returns>
    public static bool SetPrivacyMode(AutomationMesh mesh, AutomationMeshPrivacyMode privacyMode = AutomationMeshPrivacyMode.High)
    {
        if (mesh == null)
            return false;

        if (mesh.CurrentPrivacyMode.HasValue && mesh.CurrentPrivacyMode.Value == privacyMode)
            return false;

        mesh.CurrentPrivacyMode = privacyMode;
        return true;
    }

    /// <summary>
    /// Clears the current privacy mode.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <returns><see langword="true"/> when the privacy mode changed.</returns>
    public static bool ClearPrivacyMode(AutomationMesh mesh)
    {
        if (mesh == null || !mesh.CurrentPrivacyMode.HasValue)
            return false;

        mesh.CurrentPrivacyMode = null;
        return true;
    }

    /// <summary>
    /// Determines whether privacy mode is currently enabled.
    /// </summary>
    /// <param name="mesh">The mesh to inspect.</param>
    /// <returns><see langword="true"/> when a privacy mode is active.</returns>
    public static bool IsPrivacyModeEnabled(AutomationMesh mesh)
    {
        return mesh != null && mesh.CurrentPrivacyMode.HasValue;
    }

    /// <summary>
    /// Normalizes and validates a language identifier.
    /// </summary>
    /// <param name="languageId">The raw language identifier.</param>
    /// <returns>The canonical culture name, or <see langword="null"/> when invalid.</returns>
    public static string NormalizeLanguageId(string languageId)
    {
        if (string.IsNullOrWhiteSpace(languageId))
            return null;

        try
        {
            string normalizedLanguageId = CultureInfo.GetCultureInfo(languageId.Trim()).Name;
            bool exists = CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Any(culture => string.Equals(culture.Name, normalizedLanguageId, StringComparison.OrdinalIgnoreCase));

            if (!exists)
                return null;

            return normalizedLanguageId;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    /// <summary>
    /// Normalizes and validates an IANA time zone identifier.
    /// </summary>
    /// <param name="timeZoneId">The raw time zone identifier.</param>
    /// <returns>The trimmed IANA identifier, or <see langword="null"/> when invalid.</returns>
    public static string NormalizeIanaTimeZoneId(string timeZoneId)
    {
        if (string.IsNullOrWhiteSpace(timeZoneId))
            return null;

        string trimmedTimeZoneId = timeZoneId.Trim();
        if (!LooksLikeIanaTimeZoneId(trimmedTimeZoneId))
            return null;

        try
        {
            TimeZoneInfo.FindSystemTimeZoneById(trimmedTimeZoneId);
            return trimmedTimeZoneId;
        }
        catch (TimeZoneNotFoundException)
        {
            return null;
        }
        catch (InvalidTimeZoneException)
        {
            return null;
        }
    }

    private static bool LooksLikeIanaTimeZoneId(string timeZoneId)
    {
        return string.Equals(timeZoneId, "UTC", StringComparison.Ordinal)
            || string.Equals(timeZoneId, "Etc/UTC", StringComparison.Ordinal)
            || timeZoneId.Contains('/', StringComparison.Ordinal);
    }

    /// <summary>
    /// Applies mesh language and time zone settings.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="languageId">The language identifier to persist.</param>
    /// <param name="timeZoneId">The time zone identifier to persist.</param>
    /// <returns><see langword="true"/> when at least one setting changed.</returns>
    public static bool ApplySettings(AutomationMesh mesh, string languageId, string timeZoneId)
    {
        if (mesh == null)
            return false;

        bool changed = false;

        if (!string.Equals(mesh.LanguageId, languageId, StringComparison.InvariantCulture))
        {
            mesh.LanguageId = languageId;
            changed = true;
        }

        if (!string.Equals(mesh.TimeZoneId, timeZoneId, StringComparison.InvariantCulture))
        {
            mesh.TimeZoneId = timeZoneId;
            changed = true;
        }

        return changed;
    }

    /// <summary>
    /// Normalizes and validates the public base domain configured for the mesh.
    /// </summary>
    /// <param name="publicBaseDomain">The raw public base domain.</param>
    /// <returns>The normalized lower-case DNS name, or <see langword="null"/> when invalid.</returns>
    public static string NormalizePublicBaseDomain(string publicBaseDomain)
    {
        if (string.IsNullOrWhiteSpace(publicBaseDomain))
            return null;

        string normalizedDomain = publicBaseDomain.Trim().Trim('.').ToLowerInvariant();
        if (normalizedDomain.Length == 0)
            return null;

        if (normalizedDomain.Contains('/', StringComparison.Ordinal)
            || normalizedDomain.Contains(':', StringComparison.Ordinal)
            || normalizedDomain.Contains('*', StringComparison.Ordinal))
        {
            return null;
        }

        if (Uri.CheckHostName(normalizedDomain) != UriHostNameType.Dns)
            return null;

        string[] labels = normalizedDomain.Split('.');
        if (labels.Length < 2)
            return null;

        foreach (string label in labels)
        {
            if (label.Length == 0 || label.Length > 63)
                return null;

            if (label.StartsWith("-", StringComparison.Ordinal) || label.EndsWith("-", StringComparison.Ordinal))
                return null;
        }

        return normalizedDomain;
    }

    /// <summary>
    /// Normalizes and validates a country identifier.
    /// </summary>
    /// <param name="countryId">The raw country identifier.</param>
    /// <returns>The canonical ISO country code, or <see langword="null"/> when invalid.</returns>
    public static string NormalizeCountryId(string countryId)
    {
        if (string.IsNullOrWhiteSpace(countryId))
            return null;

        try
        {
            RegionInfo region = new(countryId.Trim());
            return region.TwoLetterISORegionName;
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    /// <summary>
    /// Applies a country identifier to the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="countryId">The country identifier to persist.</param>
    /// <returns><see langword="true"/> when the country changed.</returns>
    public static bool ApplyCountryId(AutomationMesh mesh, string countryId)
    {
        if (mesh == null)
            return false;

        if (string.Equals(mesh.CountryId, countryId, StringComparison.InvariantCulture))
            return false;

        mesh.CountryId = countryId;
        return true;
    }

    /// <summary>
    /// Applies the public base domain to the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="publicBaseDomain">The public base domain to persist.</param>
    /// <returns><see langword="true"/> when the domain changed.</returns>
    public static bool ApplyPublicBaseDomain(AutomationMesh mesh, string publicBaseDomain)
    {
        if (mesh == null)
            return false;

        if (string.Equals(mesh.PublicBaseDomain, publicBaseDomain, StringComparison.InvariantCulture))
            return false;

        mesh.PublicBaseDomain = publicBaseDomain;
        return true;
    }

    /// <summary>
    /// Applies a location identifier to the mesh.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="locationId">The location identifier to persist.</param>
    /// <returns><see langword="true"/> when the location changed.</returns>
    public static bool SetLocationId(AutomationMesh mesh, string locationId)
    {
        if (mesh == null || string.IsNullOrWhiteSpace(locationId))
            return false;

        string normalizedLocationId = locationId.ToLowerInvariant();

        if (string.Equals(mesh.LocationId, normalizedLocationId, StringComparison.InvariantCulture))
            return false;

        mesh.LocationId = normalizedLocationId;
        return true;
    }

    // TODO: Migrate mesh settings rules.
    // Legacy surface:
    // - GET  {name}/location
    // - POST {name}/settings
    // - POST {name}/location
    // - GET  {name}/location/set/{locationId}
    // - GET  settings/available/language
    // - GET  settings/available/timezone
    // Business decisions to preserve:
    // - normalization to the local mesh
    // - side effects on dependent agents when settings change
    // Remaining work:
    // - location existence validation against the location repository

    // TODO: Migrate privacy mode rules.
    // Legacy surface:
    // - GET local/privacymode/set
    // - GET local/privacymode/clear
    // - GET local/privacymode/isenabled
    // Remaining work:
    // - persistence
    // - side effects currently emitted to messaging and realtime hubs

    // TODO: Migrate global scenario rules.
    // Legacy surface:
    // - GET/POST/DELETE local/globalscenario/*
    // Remaining work:
    // - image storage and public URL generation
    // - persistence and side effects after current scenario changes
}