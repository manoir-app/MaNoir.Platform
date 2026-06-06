using MaNoir.CommunicationHub.Contracts.Models.Chat;
using MaNoir.Core.DataAccess;
using MongoDB.Driver;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.CommunicationHub.Chat;

/// <summary>
/// Provides MongoDB-backed operations for Communication Hub chat primitives.
/// </summary>
public sealed class CommunicationChatMongoOperations
{
    private readonly IMongoCollection<CommunicationChannel> _channelCollection;
    private readonly IMongoCollection<CommunicationMessage> _messageCollection;

    /// <summary>
    /// Initializes a new instance of the <see cref="CommunicationChatMongoOperations"/> class.
    /// </summary>
    public CommunicationChatMongoOperations()
    {
        MongoDbHelper mongo = new MongoDbHelper();
        _channelCollection = mongo.GetCollection<CommunicationChannel>();
        _messageCollection = mongo.GetCollection<CommunicationMessage>();
    }

    /// <summary>
    /// Gets one channel by identifier.
    /// </summary>
    public Task<CommunicationChannel> GetChannelByIdAsync(string channelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentException("The channel identifier cannot be empty.", nameof(channelId));

        return _channelCollection.Find(channel => channel.Id == channelId).FirstOrDefaultAsync(cancellationToken);
    }

    /// <summary>
    /// Lists channels visible to one participant.
    /// </summary>
    public Task<List<CommunicationChannel>> GetChannelsForParticipantAsync(string participantId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(participantId))
            throw new ArgumentException("The participant identifier cannot be empty.", nameof(participantId));

        return _channelCollection.Find(channel => channel.Participants.Exists(participant => participant.Id == participantId)).ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Inserts or replaces a channel by identifier.
    /// </summary>
    public Task SaveChannelAsync(CommunicationChannel channel, CancellationToken cancellationToken = default)
    {
        if (channel == null)
            throw new ArgumentNullException(nameof(channel));

        if (string.IsNullOrWhiteSpace(channel.Id))
            channel.Id = Guid.NewGuid().ToString("N");

        return _channelCollection.ReplaceOneAsync(existingChannel => existingChannel.Id == channel.Id, channel, new ReplaceOptions() { IsUpsert = true }, cancellationToken);
    }

    /// <summary>
    /// Persists one message in the target channel.
    /// </summary>
    public async Task<CommunicationMessage> AppendMessageAsync(CommunicationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
            throw new ArgumentNullException(nameof(message));

        if (string.IsNullOrWhiteSpace(message.ChannelId))
            throw new ArgumentException("The channel identifier cannot be empty.", nameof(message));

        if (string.IsNullOrWhiteSpace(message.Id))
            message.Id = Guid.NewGuid().ToString("N");

        if (message.SentAt == default)
            message.SentAt = DateTimeOffset.UtcNow;

        await _messageCollection.InsertOneAsync(message, cancellationToken: cancellationToken);
        return message;
    }

    /// <summary>
    /// Lists messages for one channel, optionally constrained by time and result size.
    /// </summary>
    public Task<List<CommunicationMessage>> GetMessagesAsync(string channelId, DateTimeOffset? since = null, int limit = 200, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentException("The channel identifier cannot be empty.", nameof(channelId));

        if (limit <= 0)
            throw new ArgumentOutOfRangeException(nameof(limit), "The message limit must be strictly positive.");

        DateTimeOffset effectiveSince = since ?? DateTimeOffset.MinValue;
        return _messageCollection
            .Find(message => message.ChannelId == channelId && message.SentAt >= effectiveSince)
            .Sort(Builders<CommunicationMessage>.Sort.Ascending(message => message.SentAt))
            .Limit(limit)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Deletes one channel and all its persisted messages.
    /// </summary>
    public async Task DeleteChannelAsync(string channelId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(channelId))
            throw new ArgumentException("The channel identifier cannot be empty.", nameof(channelId));

        await _messageCollection.DeleteManyAsync(message => message.ChannelId == channelId, cancellationToken);
        await _channelCollection.DeleteOneAsync(channel => channel.Id == channelId, cancellationToken);
    }
}