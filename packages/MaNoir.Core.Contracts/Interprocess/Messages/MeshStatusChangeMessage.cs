using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{
    public class MeshStatusChangeMessage : BaseMessage
    {
        public MeshStatusChangeMessage() : base("system.global.status.change")
        {
        }

        public string MeshId { get; set; }
        public string StatusKind { get; set; }
        public string NewStatus { get; set; }
    }
}
