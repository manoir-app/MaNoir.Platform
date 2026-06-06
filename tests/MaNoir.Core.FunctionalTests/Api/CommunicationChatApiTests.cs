using MaNoir.CommunicationHub.Contracts.Models.Chat;
using MaNoir.Core.Api;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class CommunicationChatApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task ChannelLifecycle_ShouldPersistRichMessagesAndExposeParticipantScopedReads()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        await SeedUsersAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string johnToken = await LoginAsync(client, "john");

        using HttpRequestMessage putChannelRequest = new(HttpMethod.Put, "/api/core/communication/chat/channels/household")
        {
            Content = JsonContent.Create(new CommunicationChannelUpsertRequest()
            {
                Label = "Household",
                Kind = CommunicationChannelKind.Group,
                Participants =
                [
                    new CommunicationParticipantUpsertRequest()
                    {
                        ParticipantId = "claire",
                        DisplayName = "Claire",
                        Kind = CommunicationParticipantKind.User,
                        Role = CommunicationParticipantRole.Member,
                    },
                ],
            })
        };
        putChannelRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage putChannelResponse = await client.SendAsync(putChannelRequest);
        CommunicationChannelResponse storedChannel = await putChannelResponse.Content.ReadFromJsonAsync<CommunicationChannelResponse>();

        Assert.AreEqual(HttpStatusCode.OK, putChannelResponse.StatusCode);
        Assert.IsNotNull(storedChannel);
        Assert.AreEqual("household", storedChannel.Id);
        Assert.AreEqual(2, storedChannel.Participants.Count);
        Assert.AreEqual(CommunicationParticipantRole.Owner, storedChannel.Participants.Find(participant => participant.ParticipantId == "john")?.Role);

        using HttpRequestMessage postMessageRequest = new(HttpMethod.Post, "/api/core/communication/chat/channels/household/messages")
        {
            Content = JsonContent.Create(new CommunicationMessageAppendRequest()
            {
                Kind = CommunicationMessageKind.Standard,
                Parts =
                [
                    new CommunicationMessagePartRequest()
                    {
                        Kind = CommunicationMessagePartKind.StructuredPayload,
                        MimeType = CommunicationPayloadMimeTypes.Card,
                        PayloadJson = "{\"title\":\"Filtre a remplacer\",\"summary\":\"Ventilation\"}",
                    },
                ],
            })
        };
        postMessageRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage postMessageResponse = await client.SendAsync(postMessageRequest);
        CommunicationMessageResponse storedMessage = await postMessageResponse.Content.ReadFromJsonAsync<CommunicationMessageResponse>();

        Assert.AreEqual(HttpStatusCode.OK, postMessageResponse.StatusCode);
        Assert.IsNotNull(storedMessage);
        Assert.AreEqual("john", storedMessage.SenderParticipantId);
        Assert.AreEqual("Filtre a remplacer", storedMessage.PreviewText);

        using HttpRequestMessage getMessagesRequest = new(HttpMethod.Get, "/api/core/communication/chat/channels/household/messages");
        getMessagesRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage getMessagesResponse = await client.SendAsync(getMessagesRequest);
        List<CommunicationMessageResponse> messages = await getMessagesResponse.Content.ReadFromJsonAsync<List<CommunicationMessageResponse>>();

        Assert.AreEqual(HttpStatusCode.OK, getMessagesResponse.StatusCode);
        Assert.IsNotNull(messages);
        Assert.AreEqual(1, messages.Count);
        Assert.AreEqual(CommunicationPayloadMimeTypes.Card, messages[0].Parts[0].MimeType);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PutChannel_ShouldRejectMemberTryingToManageExistingChannel()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        await SeedUsersAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string johnToken = await LoginAsync(client, "john");
        string claireToken = await LoginAsync(client, "claire");

        using HttpRequestMessage createRequest = new(HttpMethod.Put, "/api/core/communication/chat/channels/household")
        {
            Content = JsonContent.Create(new CommunicationChannelUpsertRequest()
            {
                Label = "Household",
                Kind = CommunicationChannelKind.Group,
                Participants =
                [
                    new CommunicationParticipantUpsertRequest()
                    {
                        ParticipantId = "claire",
                        DisplayName = "Claire",
                        Kind = CommunicationParticipantKind.User,
                        Role = CommunicationParticipantRole.Member,
                    },
                ],
            })
        };
        createRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage createResponse = await client.SendAsync(createRequest);

        Assert.AreEqual(HttpStatusCode.OK, createResponse.StatusCode);

        using HttpRequestMessage updateRequest = new(HttpMethod.Put, "/api/core/communication/chat/channels/household")
        {
            Content = JsonContent.Create(new CommunicationChannelUpsertRequest()
            {
                Label = "Updated household",
                Kind = CommunicationChannelKind.Group,
                Participants =
                [
                    new CommunicationParticipantUpsertRequest()
                    {
                        ParticipantId = "john",
                        DisplayName = "John",
                        Kind = CommunicationParticipantKind.User,
                        Role = CommunicationParticipantRole.Owner,
                    },
                    new CommunicationParticipantUpsertRequest()
                    {
                        ParticipantId = "claire",
                        DisplayName = "Claire",
                        Kind = CommunicationParticipantKind.User,
                        Role = CommunicationParticipantRole.Member,
                    },
                ],
            })
        };
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claireToken);

        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, updateResponse.StatusCode);
    }

    private static async Task SeedUsersAsync()
    {
        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "john",
            FirstName = "John",
            Name = "Doe",
            CommonName = "John",
            IsAdmin = false,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");
        await userLogic.SaveAsync(new User()
        {
            Id = "claire",
            FirstName = "Claire",
            Name = "Fisher",
            CommonName = "Claire",
            IsAdmin = false,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("claire", "P@ssword-42");
    }

    private static async Task<string> LoginAsync(HttpClient client, string userId)
    {
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = userId,
            Password = "P@ssword-42"
        });
        UserAuthenticationResponse payload = await loginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload?.AccessToken));
        return payload.AccessToken;
    }

    private static WebApplication CreateApplication()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(new WebApplicationOptions() { EnvironmentName = Environments.Development });
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string>()
        {
            ["MaNoir:Authentication:UsersJwt:Issuer"] = "tests.manoir.core",
            ["MaNoir:Authentication:UsersJwt:Audience"] = "tests.manoir.core.users",
            ["MaNoir:Authentication:UsersJwt:SigningKey"] = "tests-only-signing-key-value-with-more-than-32-chars",
            ["MaNoir:Authentication:UsersJwt:CookieName"] = "manoir_test_users_access_token",
            ["MaNoir:Authentication:UsersJwt:AccessTokenLifetimeMinutes"] = "120"
        });
        builder.AddMaNoirCoreApi();

        WebApplication app = builder.Build();
        app.UseMaNoirCoreApi();
        return app;
    }
}