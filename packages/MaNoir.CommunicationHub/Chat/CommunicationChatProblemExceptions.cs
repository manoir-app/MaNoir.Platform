using MaNoir.Core.Problems;

namespace MaNoir.CommunicationHub.Chat;

/// <summary>
/// Represents one invalid Communication Hub channel payload.
/// </summary>
public sealed class InvalidCommunicationChannelException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommunicationChannelException"/> class.
    /// </summary>
    /// <param name="message">Validation detail exposed through the problem payload.</param>
    public InvalidCommunicationChannelException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 400;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/communication-hub/invalid-channel";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Invalid communication channel";
}

/// <summary>
/// Represents one invalid Communication Hub message payload.
/// </summary>
public sealed class InvalidCommunicationMessageException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidCommunicationMessageException"/> class.
    /// </summary>
    /// <param name="message">Validation detail exposed through the problem payload.</param>
    public InvalidCommunicationMessageException(string message) : base(message)
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 400;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/communication-hub/invalid-message";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Invalid communication message";
}

/// <summary>
/// Represents one missing Communication Hub channel.
/// </summary>
public sealed class CommunicationChannelNotFoundException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationChannelNotFoundException"/> class.
    /// </summary>
    /// <param name="channelId">Channel identifier that could not be found.</param>
    public CommunicationChannelNotFoundException(string channelId)
        : base($"The communication channel '{channelId}' was not found.")
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 404;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/communication-hub/channel-not-found";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Communication channel not found";
}

/// <summary>
/// Represents one sender not allowed to post in the target channel.
/// </summary>
public sealed class CommunicationParticipantNotInChannelException : CoreProblemException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationParticipantNotInChannelException"/> class.
    /// </summary>
    /// <param name="participantId">Participant identifier.</param>
    /// <param name="channelId">Channel identifier.</param>
    public CommunicationParticipantNotInChannelException(string participantId, string channelId)
        : base($"The participant '{participantId}' is not attached to channel '{channelId}'.")
    {
    }

    /// <summary>
    /// Gets the HTTP status code returned for this problem.
    /// </summary>
    public override int StatusCode => 403;

    /// <summary>
    /// Gets the stable problem type URI returned for this problem.
    /// </summary>
    public override string ProblemType => "https://manoir.app/problems/communication-hub/participant-not-in-channel";

    /// <summary>
    /// Gets the short problem title returned for this problem.
    /// </summary>
    public override string ProblemTitle => "Communication participant not in channel";
}