using System;
using Home.Common;
using Home.Common.Messages;

namespace MaNoir.Core.Mesh;

internal static class AutomationMeshInterprocessPublisher
{
    public static void TryPublishPublicBaseDomainChanged(string meshId, string previousPublicBaseDomain, string publicBaseDomain)
    {
        try
        {
            NatsInterprocess.Push(new MeshPublicBaseDomainChangedMessage()
            {
                MeshId = meshId,
                PreviousPublicBaseDomain = previousPublicBaseDomain,
                PublicBaseDomain = publicBaseDomain
            });
        }
        catch (Exception exception)
        {
            Console.Error.WriteLine("The mesh public base domain change could not be published over NATS: " + exception.Message);
        }
    }
}