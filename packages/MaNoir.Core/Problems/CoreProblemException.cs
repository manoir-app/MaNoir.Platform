using System;

namespace MaNoir.Core.Problems;

/// <summary>
/// Represents one business exception that should be translated to a stable API problem payload.
/// </summary>
public abstract class CoreProblemException : Exception
{
    protected CoreProblemException(string message) : base(message)
    {
    }

    public abstract int StatusCode { get; }

    public abstract string ProblemType { get; }

    public abstract string ProblemTitle { get; }
}