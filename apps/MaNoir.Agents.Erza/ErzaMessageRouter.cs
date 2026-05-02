using Home.Common.Messages;
using MaNoir.Core.Users;

namespace MaNoir.Agents.Erza;

public sealed class ErzaMessageRouter
{
    private readonly PresenceLogic _presenceLogic;
    private readonly ErzaRuntime _runtime;

    public ErzaMessageRouter(ErzaRuntime runtime)
    {
        _presenceLogic = new PresenceLogic();
        _runtime = runtime;
    }

    public MessageResponse HandleMessage(MessageOrigin origin, string topic, string messageBody)
    {
        if (string.IsNullOrWhiteSpace(topic) || string.IsNullOrWhiteSpace(messageBody))
            return MessageResponse.GenericFail;

        switch (topic.ToLowerInvariant())
        {
            case PresenceNotificationMessage.TopicName:
                return HandleActivity(messageBody);
            case PresenceChangedMessage.TopicName:
                return HandlePresenceChanged();
            default:
                return MessageResponse.OK;
        }
    }

    private MessageResponse HandleActivity(string messageBody)
    {
        PresenceNotificationMessage message = BaseMessage.ReadAs<PresenceNotificationMessage>(messageBody);
        if (message?.Data == null)
            return MessageResponse.GenericFail;

        PresenceChangeSet changeSet = _presenceLogic.HandleActivityAsync(message.Data, _runtime.LocalLocationId).GetAwaiter().GetResult();
        _runtime.PublishPresenceChanges(changeSet);
        return MessageResponse.OK;
    }

    private MessageResponse HandlePresenceChanged()
    {
        _presenceLogic.RefreshMeshPrivacyModeAsync().GetAwaiter().GetResult();
        return MessageResponse.OK;
    }
}