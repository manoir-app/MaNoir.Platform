using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{

   
    public class MeshExtensionOperationMessage : BaseMessage
    {

        public const string TopicCreate = "system.extensions.create";
        public const string TopicInstall = "system.extensions.install";
        public const string TopicRestart = "system.extensions.restart";
        public const string TopicTerminate = "system.extensions.terminate";

        public MeshExtensionOperationMessage() : base(TopicRestart)
        {

        }

        public MeshExtensionOperationMessage(string messageTopic) : base(messageTopic)
        {
        }

        public string ExtensionId { get; set; }

        public string RepositoryUrl { get; set; }

        public string OperationId { get; set; }

        public string RequestedByUserId { get; set; }
    }

    public sealed class PluginInstallationResponse : MessageResponse
    {
        public string OperationId { get; set; }

        public string RepositoryUrl { get; set; }

        public string Status { get; set; }

        public string Message { get; set; }
    }
}
