using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{
    public class AgentGenericMessage : BaseMessage
    {
        public AgentGenericMessage(string messageTopic) : base(messageTopic)
        {
        }

        public string MessageContent { get; set; }
    }
}
