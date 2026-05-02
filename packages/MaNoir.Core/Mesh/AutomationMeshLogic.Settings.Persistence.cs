using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Locations;
using MaNoir.Core.Locations;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Mesh;

public sealed partial class AutomationMeshLogic
{
    /// <summary>
    /// Gets the frontend URL catalog from the local mesh.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The current frontend URL catalog, or an empty dictionary when no mesh exists.</returns>
    public async Task<Dictionary<string, string>> GetFrontendUrlsAsync(CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        return GetFrontendUrls(mesh);
    }

    /// <summary>
    /// Gets one frontend URL from the local mesh.
    /// </summary>
    /// <param name="frontendCode">The frontend code to find.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The matching URL, or <see langword="null"/> when missing.</returns>
    public async Task<string> GetFrontendUrlAsync(string frontendCode, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        return GetFrontendUrl(mesh, frontendCode);
    }

    /// <summary>
    /// Sets one frontend URL on the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="frontendCode">The stable frontend code.</param>
    /// <param name="frontendUrl">The absolute frontend URL.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the catalog changed.</returns>
    public async Task<bool> SetFrontendUrlAsync(string frontendCode, string frontendUrl, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = UpsertFrontendUrl(mesh, frontendCode, frontendUrl);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Deletes one frontend URL from the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="frontendCode">The stable frontend code.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the catalog changed.</returns>
    public async Task<bool> DeleteFrontendUrlAsync(string frontendCode, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = DeleteFrontendUrl(mesh, frontendCode);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Creates or updates a global scenario on the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="scenario">The incoming scenario payload.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The stored scenario, or <see langword="null"/> when the local mesh or scenario is missing.</returns>
    public async Task<AutomationMeshGlobalScenario> UpsertGlobalScenarioAsync(AutomationMeshGlobalScenario scenario, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return null;

        AutomationMeshGlobalScenario existingScenario = GetGlobalScenario(mesh, scenario == null ? null : scenario.Code);
        AutomationMeshGlobalScenario storedScenario = UpsertGlobalScenario(mesh, scenario);

        if (storedScenario == null)
            return null;

        if (!ReferenceEquals(existingScenario, storedScenario))
            await SaveAsync(mesh, cancellationToken);

        return storedScenario;
    }

    /// <summary>
    /// Deletes a global scenario from the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="scenarioCode">The scenario code to delete.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when a scenario was removed.</returns>
    public async Task<bool> DeleteGlobalScenarioAsync(string scenarioCode, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = DeleteGlobalScenario(mesh, scenarioCode);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Sets the current global scenario on the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="scenarioCode">The scenario code to activate.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the current scenario changed.</returns>
    public async Task<bool> SetCurrentGlobalScenarioAsync(string scenarioCode, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = SetCurrentGlobalScenario(mesh, scenarioCode);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Clears the current global scenario on the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the current scenario changed.</returns>
    public async Task<bool> ClearCurrentGlobalScenarioAsync(CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = ClearCurrentGlobalScenario(mesh);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Applies a privacy mode to the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="privacyMode">The privacy mode to apply.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the privacy mode changed.</returns>
    public async Task<bool> SetPrivacyModeAsync(AutomationMeshPrivacyMode privacyMode = AutomationMeshPrivacyMode.High, CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = SetPrivacyMode(mesh, privacyMode);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Clears the privacy mode on the local mesh and persists the change when needed.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the privacy mode changed.</returns>
    public async Task<bool> ClearPrivacyModeAsync(CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        if (mesh == null)
            return false;

        bool changed = ClearPrivacyMode(mesh);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Determines whether privacy mode is enabled on the local mesh.
    /// </summary>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when privacy mode is enabled.</returns>
    public async Task<bool> IsPrivacyModeEnabledAsync(CancellationToken cancellationToken = default)
    {
        AutomationMesh mesh = await GetLocalAsync(cancellationToken);
        return IsPrivacyModeEnabled(mesh);
    }

    /// <summary>
    /// Updates mesh language and time zone settings and persists the change when needed.
    /// </summary>
    /// <param name="meshId">The mesh identifier to update.</param>
    /// <param name="languageId">The language identifier to persist.</param>
    /// <param name="timeZoneId">The time zone identifier to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the settings changed.</returns>
    public async Task<bool> UpdateSettingsAsync(string meshId, string languageId, string timeZoneId, CancellationToken cancellationToken = default)
    {
        string normalizedMeshId = NormalizeMeshId(meshId);
        if (normalizedMeshId == null)
            return false;

        string normalizedLanguageId = NormalizeLanguageId(languageId);
        string normalizedTimeZoneId = NormalizeIanaTimeZoneId(timeZoneId);
        if (normalizedLanguageId == null || normalizedTimeZoneId == null)
            return false;

        AutomationMesh mesh = await GetByIdAsync(normalizedMeshId, cancellationToken);
        if (mesh == null)
            return false;

        bool changed = ApplySettings(mesh, normalizedLanguageId, normalizedTimeZoneId);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }

    /// <summary>
    /// Updates the location identifier of a mesh and persists the change when needed.
    /// </summary>
    /// <param name="meshId">The mesh identifier to update.</param>
    /// <param name="locationId">The location identifier to persist.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns><see langword="true"/> when the location changed.</returns>
    public async Task<bool> SetLocationAsync(string meshId, string locationId, CancellationToken cancellationToken = default)
    {
        string normalizedMeshId = NormalizeMeshId(meshId);
        if (normalizedMeshId == null)
            return false;

        AutomationMesh mesh = await GetByIdAsync(normalizedMeshId, cancellationToken);
        if (mesh == null)
            return false;

        string normalizedLocationId = LocationLogic.NormalizeLocationId(locationId);
        if (normalizedLocationId == null)
            return false;

        Location location = await _locationMongoOperations.GetByIdAsync(normalizedLocationId, cancellationToken);
        if (location == null)
            return false;

        bool changed = SetLocationId(mesh, location.Id);
        if (changed)
            await SaveAsync(mesh, cancellationToken);

        return changed;
    }
}