using Home.Common.Messages;
using MaNoir.Agents.Erza;
using MaNoir.CommunicationHub.Chat;
using MaNoir.CommunicationHub.Contracts.Models.Chat;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Agents;

[TestClass]
[DoNotParallelize]
public sealed class ErzaSecurityMessageRouterTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task UserLoginFailedMessage_ShouldPublishSecurityAlertWhenThresholdIsReached()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "eric",
            IsGuest = false,
            IsMain = true,
            IsAdmin = false
        });
        await userLogic.SetPasswordAsync("eric", "P@ssword-42");
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            IsGuest = false,
            IsMain = true,
            IsAdmin = true,
            CommonName = "Sarah"
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");

        UserFailedLoginStateTracker tracker = new UserFailedLoginStateTracker();
        for (int i = 0; i < 5; i++)
        {
            await tracker.RegisterFailedLoginAttemptAsync("eric", "127.0.0.1", "tests");
        }

        ErzaMessageRouter router = new ErzaMessageRouter(new ErzaRuntime());
        UserLoginFailedMessage message = new UserLoginFailedMessage()
        {
            UserId = "eric",
            FailedCount = 5
        };

        MessageResponse response = router.HandleMessage(MessageOrigin.System, UserLoginFailedMessage.TopicName, JsonConvert.SerializeObject(message));

        CommunicationChatLogic chatLogic = new CommunicationChatLogic();
        CommunicationChannel channel = await chatLogic.GetChannelByIdAsync(ErzaSecurityCommunicationPublisher.SecurityChannelId);
        List<CommunicationMessage> messages = await chatLogic.GetMessagesAsync(ErzaSecurityCommunicationPublisher.SecurityChannelId);

        Assert.IsNotNull(response);
        Assert.AreEqual("ok", response.Response);
        Assert.IsNotNull(channel);
        Assert.AreEqual(CommunicationChannelKind.System, channel.Kind);
        Assert.IsTrue(channel.Participants.Exists(participant => participant.Id == ErzaSecurityCommunicationPublisher.ErzaParticipantId));
        Assert.IsTrue(channel.Participants.Exists(participant => participant.Id == "sarah"));
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(ErzaSecurityCommunicationPublisher.ErzaParticipantId, messages[0].SenderParticipantId);
        Assert.AreEqual(CommunicationMessageKind.Event, messages[0].Kind);
        Assert.AreEqual("Trop de tentatives de connexion pour eric", messages[0].PreviewText);
        Assert.AreEqual(CommunicationPayloadMimeTypes.SystemEvent, messages[0].Parts[0].MimeType);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UserPasswordChangedMessage_ShouldPublishSecurityEvent()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            IsGuest = false,
            IsMain = true,
            IsAdmin = true,
            CommonName = "Sarah"
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");

        ErzaMessageRouter router = new ErzaMessageRouter(new ErzaRuntime());
        UserPasswordChangedMessage message = new UserPasswordChangedMessage()
        {
            UserId = "sarah",
            RemoteAddress = "127.0.0.1",
            UserAgent = "tests"
        };

        MessageResponse response = router.HandleMessage(MessageOrigin.System, UserPasswordChangedMessage.TopicName, JsonConvert.SerializeObject(message));

        CommunicationChatLogic chatLogic = new CommunicationChatLogic();
        CommunicationChannel channel = await chatLogic.GetChannelByIdAsync(ErzaSecurityCommunicationPublisher.SecurityChannelId);
        List<CommunicationMessage> messages = await chatLogic.GetMessagesAsync(ErzaSecurityCommunicationPublisher.SecurityChannelId);

        Assert.IsNotNull(response);
        Assert.AreEqual("ok", response.Response);
        Assert.IsNotNull(channel);
        Assert.AreEqual(CommunicationChannelKind.System, channel.Kind);
        Assert.IsTrue(channel.Participants.Exists(participant => participant.Id == ErzaSecurityCommunicationPublisher.ErzaParticipantId));
        Assert.IsTrue(channel.Participants.Exists(participant => participant.Id == "sarah"));
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(ErzaSecurityCommunicationPublisher.ErzaParticipantId, messages[0].SenderParticipantId);
        Assert.AreEqual(CommunicationMessageKind.Event, messages[0].Kind);
        Assert.AreEqual("Mot de passe mis a jour pour sarah", messages[0].PreviewText);
        Assert.AreEqual(CommunicationPayloadMimeTypes.SystemEvent, messages[0].Parts[0].MimeType);
    }
}