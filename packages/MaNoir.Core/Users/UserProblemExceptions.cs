using MaNoir.Core.Problems;

namespace MaNoir.Core.Users;

/// <summary>
/// Represents one invalid user credentials failure.
/// </summary>
public sealed class InvalidUserCredentialsException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidUserCredentialsException"/> class.
    /// </summary>
    public InvalidUserCredentialsException() : base("The supplied user credentials are invalid.")
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 401;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/auth/invalid-user-credentials";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Invalid user credentials";
}

/// <summary>
/// Represents one invalid user password payload.
/// </summary>
public sealed class InvalidUserPasswordException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidUserPasswordException"/> class.
    /// </summary>
    /// <param name="message">Validation detail exposed through the problem payload.</param>
    public InvalidUserPasswordException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 400;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/auth/invalid-user-password";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Invalid user password";
}