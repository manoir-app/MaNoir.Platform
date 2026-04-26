using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{
    public enum ItemChangeKind
    {
        Update,
        Delete
    }

    public class ItemChangeMessage : BaseMessage
    {
        public ItemChangeMessage(string messageTopic) : base(messageTopic)
        {
        }

        public string ItemId { get; set; }
        public ItemChangeKind Kind { get; set; }
    }
}
