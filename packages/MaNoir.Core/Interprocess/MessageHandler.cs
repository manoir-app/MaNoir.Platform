using Home.Common.Messages;

namespace Home.Common;

/// <summary>
/// Represents one interprocess message handler delegate.
/// </summary>
/// <param name="origin">Origin of the incoming message.</param>
/// <param name="topic">NATS topic of the incoming message.</param>
/// <param name="messageBody">Serialized message body.</param>
/// <returns>The response to send back to the requester, or <see langword="null"/> when no reply is required.</returns>
public delegate MessageResponse MessageHandler(MessageOrigin origin, string topic, string messageBody);