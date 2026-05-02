using System;

namespace MaNoir.Core.Problems;

/// <summary>
/// Represents one business exception that should be translated to a stable API problem payload.
/// </summary>
public abstract class CoreProblemException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CoreProblemException"/> class.
    /// </summary>
    /// <param name="message">Human-readable exception message.</param>
    protected CoreProblemException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the HTTP status code that should be exposed in the problem payload.
    /// </summary>
    public abstract int StatusCode { get; }

    /// <summary>
    /// Gets the stable problem type URI.
    /// </summary>
    public abstract string ProblemType { get; }

    /// <summary>
    /// Gets the short problem title exposed to API clients.
    /// </summary>
    public abstract string ProblemTitle { get; }
}