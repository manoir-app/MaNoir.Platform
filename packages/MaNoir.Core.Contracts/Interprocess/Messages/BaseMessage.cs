using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Home.Common.Messages
{
    public abstract class BaseMessage
    {
        private class EmptyMessage : BaseMessage
        {
            public EmptyMessage() : base("")
            {

            }
        }

        public static T ReadAs<T>(string messageAsJson) where T : BaseMessage, new()
        {
            return JsonConvert.DeserializeObject<T>(messageAsJson);
        }

        public static string GetTopic(string messageAsJson)
        {
            EmptyMessage msg = JsonConvert.DeserializeObject<EmptyMessage>(messageAsJson);
            if (msg == null)
                return null;
            return msg.Topic;
        }


        protected BaseMessage(string messageTopic)
        {
            Topic = messageTopic;
        }

        public string Topic { get; set; }

    }

    public enum MessageOrigin
    {
        External,
        System,
        Local
    }

    public class MessageResponse
    {
        public string Topic { get; set; }
        public string Response { get; set; }

        public static MessageResponse OK
        {
            get
            {
                return new MessageResponse() { Response = "ok" };
            }
        }

        public static MessageResponse GenericFail
        {
            get
            {
                return new MessageResponse() { Response = "fail" };
            }
        }

        public bool IsFail()
        {
            return this.Response.Equals("fail", StringComparison.InvariantCultureIgnoreCase);
        }
    }

}
