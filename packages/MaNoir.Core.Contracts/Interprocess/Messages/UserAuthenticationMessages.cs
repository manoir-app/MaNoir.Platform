using System;

namespace Home.Common.Messages
{
    public sealed class UserLoginFailedMessage : BaseMessage
    {
        public const string TopicName = "system.auth.users.login.failed";

        public UserLoginFailedMessage() : base(TopicName)
        {
        }

        public string UserId { get; set; }

        public int FailedCount { get; set; }

        public DateTimeOffset FailedAtUtc { get; set; }

        public DateTimeOffset WindowStartedAtUtc { get; set; }

        public string RemoteAddress { get; set; }

        public string UserAgent { get; set; }
    }

    public sealed class UserPasswordChangedMessage : BaseMessage
    {
        public const string TopicName = "system.auth.users.password.changed";

        public UserPasswordChangedMessage() : base(TopicName)
        {
        }

        public string UserId { get; set; }

        public DateTimeOffset ChangedAtUtc { get; set; }

        public string RemoteAddress { get; set; }

        public string UserAgent { get; set; }
    }
}