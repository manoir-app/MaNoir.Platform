using MaNoir.Core.Problems;

namespace MaNoir.Core.Setup;

/// <summary>
/// Represents one first setup operation that is no longer available.
/// </summary>
public sealed class InitialSetupUnavailableException : CoreProblemException
{
    public InitialSetupUnavailableException() : base("The first setup is no longer available because a mesh or at least one user already exists.")
    {
    }

    public override int StatusCode => 409;

    public override string ProblemType => "https://manoir.app/problems/setup/initialization-unavailable";

    public override string ProblemTitle => "Initial setup is no longer available";
}