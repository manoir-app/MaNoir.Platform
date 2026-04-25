using MaNoir.Core.Secrets;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Implements the local installed plugin and contribution catalog logic.
/// </summary>
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