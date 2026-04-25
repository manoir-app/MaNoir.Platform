using MaNoir.Core.Problems;

namespace MaNoir.Core.Users;

/// <summary>
/// Represents one invalid user credentials failure.
/// </summary>
public sealed class InvalidUserCredentialsException : CoreProblemException
{
    public InvalidUserCredentialsException() : base("The supplied user credentials are invalid.")
    {
    }

    public override int StatusCode => 401;

    public override string ProblemType => "https://manoir.app/problems/auth/invalid-user-credentials";

    public override string ProblemTitle => "Invalid user credentials";
}

/// <summary>
/// Represents one invalid user password payload.
/// </summary>
public sealed class InvalidUserPasswordException : CoreProblemException
{
    public InvalidUserPasswordException(string message) : base(message)
    {
    }

    public override int StatusCode => 400;

    public override string ProblemType => "https://manoir.app/problems/auth/invalid-user-password";

    public override string ProblemTitle => "Invalid user password";
}