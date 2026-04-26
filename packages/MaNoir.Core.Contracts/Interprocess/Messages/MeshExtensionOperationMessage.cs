using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{

   
    public class MeshExtensionOperationMessage : BaseMessage
    {

        public const string TopicCreate = "system.extensions.create";
        public const string TopicRestart = "system.extensions.restart";
        public const string TopicTerminate = "system.extensions.terminate";

        public MeshExtensionOperationMessage() : base(TopicRestart)
        {

        }

        public MeshExtensionOperationMessage(string messageTopic) : base(messageTopic)
        {
        }

        public string ExtensionId { get; set; }
    }
}
