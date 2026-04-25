using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{
    public class MeshInfoMessage : BaseMessage
    {
        public MeshInfoMessage() : base("erza.getmeshinfos")
        {
        }
    }

    public class MeshInfoMessageResponse : MessageResponse
    {
        public MeshInfoMessageResponse()
        {
            SupportedLanguages = new List<string>();
            AvailableTimeZones = new Dictionary<string, string>();
        }

        public List<string> SupportedLanguages { get; set; }
        public Dictionary<string, string> AvailableTimeZones { get; set; }
        public string KubernetesNamespace { get; set; }
        public string KubernetesServer { get; set; }
    }
}
