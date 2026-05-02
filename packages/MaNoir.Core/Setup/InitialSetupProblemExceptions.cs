using MaNoir.Core.Problems;

namespace MaNoir.Core.Setup;

/// <summary>
/// Represents one first setup operation that is no longer available.
/// </summary>
public sealed class InitialSetupUnavailableException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InitialSetupUnavailableException"/> class.
    /// </summary>
    public InitialSetupUnavailableException() : base("The first setup is no longer available because a mesh or at least one user already exists.")
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 409;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/setup/initialization-unavailable";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Initial setup is no longer available";
}