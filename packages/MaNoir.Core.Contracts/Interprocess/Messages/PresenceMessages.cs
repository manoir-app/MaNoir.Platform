using MaNoir.Core.Contracts.Models.Users;

namespace Home.Common.Messages
{
    public sealed class PresenceNotificationMessage : BaseMessage
    {
        public const string TopicName = "users.presence.activity";

        public PresenceNotificationMessage() : base(TopicName)
        {
            Data = new PresenceNotificationData();
        }

        public PresenceNotificationData Data { get; set; }
    }

    public sealed class PresenceChangedMessage : BaseMessage
    {
        public const string TopicName = "users.presence.changed";

        public PresenceChangedMessage() : base(TopicName)
        {
        }

        public string UserId { get; set; }
    }
}