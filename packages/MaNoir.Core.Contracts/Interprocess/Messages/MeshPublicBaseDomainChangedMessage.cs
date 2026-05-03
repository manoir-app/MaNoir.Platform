namespace Home.Common.Messages
{
    public sealed class MeshPublicBaseDomainChangedMessage : BaseMessage
    {
        public const string TopicName = "system.mesh.publicbasedomain.changed";

        public MeshPublicBaseDomainChangedMessage() : base(TopicName)
        {
        }

        public string MeshId { get; set; }

        public string PreviousPublicBaseDomain { get; set; }

        public string PublicBaseDomain { get; set; }
    }
}