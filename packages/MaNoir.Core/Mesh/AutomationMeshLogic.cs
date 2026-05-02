namespace MaNoir.Core.Mesh;

/// <summary>
/// Implements the mesh business logic layer on top of persistence helpers.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// AutomationMeshLogic logic = new AutomationMeshLogic();
/// AutomationMesh mesh = await logic.GetOrCreateLocalAsync(Environment.MachineName, "https://core.local/api/graph/", cancellationToken);
/// await logic.SetFrontendUrlAsync("home", "https://home.manoir.local/", cancellationToken);
/// </code>
/// <para>
/// This logic centralizes local mesh bootstrap, settings, frontend URLs, privacy mode, and runtime integration state.
/// </para>
/// </remarks>
public sealed partial class AutomationMeshLogic
{
}