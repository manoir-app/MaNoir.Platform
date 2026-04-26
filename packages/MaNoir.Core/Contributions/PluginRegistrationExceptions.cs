using System;

namespace MaNoir.Core.Contributions;

/// <summary>
/// Represents one invalid plugin descriptor publication request.
/// </summary>
public sealed class InvalidPluginDescriptorException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPluginDescriptorException"/> class.
    /// </summary>
    public InvalidPluginDescriptorException(string message)
        : base(message)
    {
    }
}