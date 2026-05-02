using MaNoir.Core.Secrets;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Implements the local installed plugin and contribution catalog logic.
/// </summary>
/// <remarks>
/// <para>Example:</para>
/// <code>
/// ContributionLogic logic = new ContributionLogic();
/// List&lt;InstalledPlugin&gt; plugins = await logic.GetInstalledPluginsAsync(cancellationToken);
/// ContributionConfigurationResponse response = await logic.ConfigureContributionInstanceAsync("mqtt.main", setupValues, cancellationToken);
/// </code>
/// <para>
/// This logic groups catalog publication, instance configuration, and contribution secret exchange for installed plugins.
/// </para>
/// </remarks>
public sealed partial class ContributionLogic
{
    private readonly ContributionMongoOperations _mongoOperations;
    private readonly SharedSecretLogic _sharedSecretLogic;

    /// <summary>
    /// Initializes a new instance of the <see cref="ContributionLogic"/> class.
    /// </summary>
    public ContributionLogic()
    {
        _mongoOperations = new ContributionMongoOperations();
        _sharedSecretLogic = new SharedSecretLogic();
    }
}