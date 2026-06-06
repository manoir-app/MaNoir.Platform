using System;
using System.Collections.Generic;

namespace MaNoir.CommunicationHub.Contracts.Models.Chat;

/// <summary>
/// Represents a conversation surface owned by the Communication Hub.
/// </summary>
public sealed class CommunicationChannel
{
    /// <summary>
    /// Initializes a new channel contract instance.
    /// </summary>
    public CommunicationChannel()
    {
        Participants = [];
        Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets or sets the canonical channel identifier.
    /// </summary>
    public string Id { get; set; }

    /// <summary>
    /// Gets or sets the display label for the channel.
    /// </summary>
    public string Label { get; set; }

    /// <summary>
    /// Gets or sets the technical shape of the channel.
    /// </summary>
    public CommunicationChannelKind Kind { get; set; }

    /// <summary>
    /// Gets or sets the participants currently attached to the channel.
    /// </summary>
    public List<CommunicationParticipant> Participants { get; set; }

    /// <summary>
    /// Gets or sets additional hub-level metadata for routing or correlation.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; }
}