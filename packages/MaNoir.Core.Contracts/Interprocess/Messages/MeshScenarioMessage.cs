using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{
    public class MeshScenarioMessage : BaseMessage
    {
        public const string ChangedTopic = "system.mesh.globalscenario.changed";
        public const string SetTopic = "system.mesh.globalscenario.set";
        public MeshScenarioMessage() : this(ChangedTopic)
        {
        }

        public MeshScenarioMessage(string topic) : base(topic)
        {

        }

        public string Scenario { get; set; }

    }
}
