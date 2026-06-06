using System;
using System.Collections.Generic;

namespace MaNoir.CommunicationHub.Contracts.Models.Chat;

/// <summary>
/// Represents a participant referenced by a Communication Hub channel.
/// </summary>
public sealed class CommunicationParticipant
{
    /// <summary>
    /// Initializes a new participant contract instance.
    /// </summary>
    public CommunicationParticipant()
    {
        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the canonical participant identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display name exposed to channel consumers.
    /// </summary>
    public string DisplayName { get; set; }

    /// <summary>
    /// Gets or sets the participant source category.
    /// </summary>
    public CommunicationParticipantKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the participant role inside the channel.
    /// </summary>
    public CommunicationParticipantRole Role { get; set; }

    /// <summary>
    /// Gets or sets additional participant metadata.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
}