using MaNoir.CommunicationHub.Contracts.Models.Chat;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.CommunicationHub.Chat;

public sealed partial class CommunicationChatLogic
{
    /// <summary>
    /// Gets one channel by identifier.
    /// </summary>
    public Task<CommunicationChannel> GetChannelByIdAsync(string channelId, CancellationToken cancellationToken = default)
    {
        string normalizedChannelId = NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return Task.FromResult<CommunicationChannel>(null);

        return _mongoOperations.GetChannelByIdAsync(normalizedChannelId, cancellationToken);
    }

    /// <summary>
    /// Gets the channels visible to one participant.
    /// </summary>
    public Task<List<CommunicationChannel>> GetChannelsForParticipantAsync(string participantId, CancellationToken cancellationToken = default)
    {
        string normalizedParticipantId = NormalizeParticipantId(participantId);
        if (normalizedParticipantId == null)
            return Task.FromResult(new List<CommunicationChannel>());

        return _mongoOperations.GetChannelsForParticipantAsync(normalizedParticipantId, cancellationToken);
    }

    /// <summary>
    /// Creates or updates a channel and persists it.
    /// </summary>
    public async Task<CommunicationChannel> UpsertChannelAsync(CommunicationChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel == null)
            return null;

        PrepareChannelForSave(channel);
        await _mongoOperations.SaveChannelAsync(channel, cancellationToken);
        return await GetChannelByIdAsync(channel.Id, cancellationToken);
    }

    /// <summary>
    /// Persists one message after validating the target channel membership rules.
    /// </summary>
    public async Task<CommunicationMessage> AppendMessageAsync(CommunicationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            return null;

        PrepareMessageForSave(message);

        CommunicationChannel channel = await _mongoOperations.GetChannelByIdAsync(message.ChannelId, cancellationToken);
        if (channel == null)
            throw new CommunicationChannelNotFoundException(message.ChannelId);

        bool isAttachedParticipant = channel.Participants != null
            && channel.Participants.Any(participant => participant != null && participant.Id == message.SenderParticipantId);

        if (!isAttachedParticipant)
            throw new CommunicationParticipantNotInChannelException(message.SenderParticipantId, message.ChannelId);

        return await _mongoOperations.AppendMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// Lists messages for one channel.
    /// </summary>
    public Task<List<CommunicationMessage>> GetMessagesAsync(string channelId, System.DateTimeOffset? since = null, int limit = 200, CancellationToken cancellationToken = default)
    {
        string normalizedChannelId = NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return Task.FromResult(new List<CommunicationMessage>());

        return _mongoOperations.GetMessagesAsync(normalizedChannelId, since, limit, cancellationToken);
    }

    /// <summary>
    /// Deletes one channel and its persisted messages.
    /// </summary>
    public async Task<bool> DeleteChannelAsync(string channelId, CancellationToken cancellationToken = default)
    {
        string normalizedChannelId = NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return false;

        CommunicationChannel existingChannel = await _mongoOperations.GetChannelByIdAsync(normalizedChannelId, cancellationToken);
        if (existingChannel == null)
            return false;

        await _mongoOperations.DeleteChannelAsync(normalizedChannelId, cancellationToken);
        return true;
    }
}