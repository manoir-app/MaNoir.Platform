using MaNoir.Core.Api;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Home.Common.Messages;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NATS.Client;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class UserAuthenticationApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task Login_WithInteractiveSession_ShouldSetCookieAndReturnUserWithoutToken()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            FirstName = "Sarah",
            Name = "Connor",
            CommonName = "Sarah",
            IsAdmin = true,
            IsMain = true,
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=true", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        });

        UserAuthenticationResponse payload = await loginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();
        string setCookieHeader = loginResponse.Headers.GetValues("Set-Cookie").Single();
        string cookieValue = setCookieHeader.Split(';')[0];

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.IsNotNull(payload);
        Assert.AreEqual("Bearer", payload.TokenType);
        Assert.IsTrue(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.IsTrue(setCookieHeader.StartsWith("manoir_test_users_access_token=", System.StringComparison.OrdinalIgnoreCase));
        Assert.IsTrue(setCookieHeader.Contains("httponly", System.StringComparison.OrdinalIgnoreCase));
        Assert.IsNull(payload.User.HashedPassword);
        Assert.IsNull(payload.User.HashedPinCode);

        using HttpRequestMessage cookieRequest = new(HttpMethod.Get, "/api/core/auth/users/me");
        cookieRequest.Headers.Add("Cookie", cookieValue);
        HttpResponseMessage cookieResponse = await client.SendAsync(cookieRequest);
        User cookieUser = await cookieResponse.Content.ReadFromJsonAsync<User>();

        Assert.AreEqual(HttpStatusCode.OK, cookieResponse.StatusCode);
        Assert.AreEqual("sarah", cookieUser.Id);
        Assert.IsNull(cookieUser.HashedPassword);
        Assert.IsNull(cookieUser.HashedPinCode);

        using HttpRequestMessage logoutRequest = new(HttpMethod.Post, "/api/core/auth/users/logout");
        logoutRequest.Headers.Add("Cookie", cookieValue);
        HttpResponseMessage logoutResponse = await client.SendAsync(logoutRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, logoutResponse.StatusCode);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Login_WithoutInteractiveSession_ShouldReturnBearerTokenAcceptedByMe()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        });

        UserAuthenticationResponse payload = await loginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.IsNotNull(payload);
        Assert.IsFalse(string.IsNullOrWhiteSpace(payload.AccessToken));
        Assert.IsFalse(loginResponse.Headers.TryGetValues("Set-Cookie", out _));

        using HttpRequestMessage bearerRequest = new(HttpMethod.Get, "/api/core/auth/users/me");
        bearerRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", payload.AccessToken);
        HttpResponseMessage bearerResponse = await client.SendAsync(bearerRequest);
        User bearerUser = await bearerResponse.Content.ReadFromJsonAsync<User>();

        Assert.AreEqual(HttpStatusCode.OK, bearerResponse.StatusCode);
        Assert.AreEqual("sarah", bearerUser.Id);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Login_ShouldRejectInvalidPassword()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=true", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "wrong-password"
        });

        ProblemDetails problem = await loginResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.AreEqual(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.IsNotNull(problem);
        Assert.AreEqual(401, problem.Status);
        Assert.AreEqual("application/problem+json", loginResponse.Content.Headers.ContentType?.MediaType);
        Assert.AreEqual("https://manoir.app/problems/auth/invalid-user-credentials", problem.Type);
        Assert.AreEqual("Invalid user credentials", problem.Title);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task Login_ShouldPublishFailedLoginEventToNatsAndPersistState()
    {
        await using NatsFunctionalTestHost natsHost = new NatsFunctionalTestHost();
        await natsHost.StartAsync();
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope natsHostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", natsHost.Host);
        using ProcessEnvironmentVariableScope natsPortScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", natsHost.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "eric",
            IsGuest = false,
            IsMain = true
        });
        await userLogic.SetPasswordAsync("eric", "P@ssword-42");

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(natsHost.ConnectionString);
        using ISyncSubscription subscription = connection.SubscribeSync(UserLoginFailedMessage.TopicName);
        connection.Flush();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "eric",
            Password = "wrong-password"
        });

        Msg message = subscription.NextMessage(5000);
        UserLoginFailedMessage payload = BaseMessage.ReadAs<UserLoginFailedMessage>(Encoding.UTF8.GetString(message.Data));
        UserFailedLoginState state = await new UserFailedLoginStateTracker().GetAsync("eric");

        Assert.AreEqual(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
        Assert.IsNotNull(payload);
        Assert.AreEqual(UserLoginFailedMessage.TopicName, message.Subject);
        Assert.AreEqual("eric", payload.UserId);
        Assert.AreEqual(1, payload.FailedCount);
        Assert.IsNotNull(state);
        Assert.AreEqual(1, state.FailedCount);
        Assert.AreEqual("eric", state.UserId);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task ChangePassword_ShouldInvalidateThePreviousPasswordAndAcceptTheNewOne()
    {
        await using NatsFunctionalTestHost natsHost = new NatsFunctionalTestHost();
        await natsHost.StartAsync();
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope natsHostScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_HOST", natsHost.Host);
        using ProcessEnvironmentVariableScope natsPortScope = new ProcessEnvironmentVariableScope("NATS_SERVICE_PORT", natsHost.Port.ToString());
        using ProcessEnvironmentVariableScope compatPortScope = new ProcessEnvironmentVariableScope("NATS_PORT_4222_TCP_PROTO", null);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");

        ConnectionFactory factory = new ConnectionFactory();
        using IConnection connection = factory.CreateConnection(natsHost.ConnectionString);
        using ISyncSubscription subscription = connection.SubscribeSync(UserPasswordChangedMessage.TopicName);
        connection.Flush();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        HttpResponseMessage loginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        });
        UserAuthenticationResponse loginPayload = await loginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage changePasswordRequest = new(HttpMethod.Post, "/api/core/auth/users/change-password");
        changePasswordRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.AccessToken);
        changePasswordRequest.Content = JsonContent.Create(new UserChangePasswordRequest()
        {
            CurrentPassword = "P@ssword-42",
            NewPassword = "N3w-P@ssword-42"
        });

        HttpResponseMessage changePasswordResponse = await client.SendAsync(changePasswordRequest);
        Msg passwordChangedMessage = subscription.NextMessage(5000);
        UserPasswordChangedMessage passwordChangedPayload = BaseMessage.ReadAs<UserPasswordChangedMessage>(Encoding.UTF8.GetString(passwordChangedMessage.Data));

        Assert.AreEqual(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);
        Assert.IsNotNull(passwordChangedPayload);
        Assert.AreEqual(UserPasswordChangedMessage.TopicName, passwordChangedMessage.Subject);
        Assert.AreEqual("sarah", passwordChangedPayload.UserId);

        HttpResponseMessage oldPasswordLoginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        });
        ProblemDetails oldPasswordProblem = await oldPasswordLoginResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.AreEqual(HttpStatusCode.Unauthorized, oldPasswordLoginResponse.StatusCode);
        Assert.IsNotNull(oldPasswordProblem);
        Assert.AreEqual("https://manoir.app/problems/auth/invalid-user-credentials", oldPasswordProblem.Type);

        HttpResponseMessage newPasswordLoginResponse = await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "N3w-P@ssword-42"
        });
        UserAuthenticationResponse newPasswordPayload = await newPasswordLoginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        Assert.AreEqual(HttpStatusCode.OK, newPasswordLoginResponse.StatusCode);
        Assert.IsFalse(string.IsNullOrWhiteSpace(newPasswordPayload.AccessToken));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task MeAccess_ShouldReturnExplicitAccessesForTheAuthenticatedUser()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User()
        {
            Id = "sarah",
            IsGuest = false
        });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
        await new AuthorizationLogic().ReplaceUserAuthorizationAsync("sarah",
        [
            new UserZoneAccess() { ZoneId = "core.admin-ui.settings", Level = AccessLevel.Contribute }
        ]);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        UserAuthenticationResponse loginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/core/auth/users/me/access");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);
        UserAuthorizationProfile profile = await response.Content.ReadFromJsonAsync<UserAuthorizationProfile>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(profile);
        Assert.AreEqual("sarah", profile.UserId);
        Assert.IsFalse(profile.IsMain);
        Assert.AreEqual(1, profile.Accesses.Count);
        Assert.AreEqual("core.admin-ui.settings", profile.Accesses[0].ZoneId);
        Assert.AreEqual(AccessLevel.Contribute, profile.Accesses[0].Level);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetCurrentAdmin_ShouldReturnTheCurrentAdminWithoutRequiringUserEnumeration()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = true });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "john", IsGuest = false, IsMain = true, IsAdmin = false });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        UserAuthenticationResponse loginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "john",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/core/auth/users/admin");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);
        User adminUser = await response.Content.ReadFromJsonAsync<User>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(adminUser);
        Assert.AreEqual("sarah", adminUser.Id);
        Assert.IsTrue(adminUser.IsAdmin);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task GetAccessZones_ShouldReturnThePublishedRightsCatalogOnlyToAuthorizedUsers()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = true });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "john", IsGuest = false, IsMain = true, IsAdmin = false });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        UserAuthenticationResponse johnLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "john",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage forbiddenRequest = new(HttpMethod.Get, "/api/core/auth/users/access-zones");
        forbiddenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnLoginPayload.AccessToken);
        HttpResponseMessage forbiddenResponse = await client.SendAsync(forbiddenRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        UserAuthenticationResponse sarahLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage request = new(HttpMethod.Get, "/api/core/auth/users/access-zones?pluginId=core");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sarahLoginPayload.AccessToken);
        HttpResponseMessage response = await client.SendAsync(request);
        List<AccessZoneDefinition> definitions = await response.Content.ReadFromJsonAsync<List<AccessZoneDefinition>>();

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.IsNotNull(definitions);
        Assert.AreEqual(5, definitions.Count);
        CollectionAssert.AreEquivalent(
            new[]
            {
                CoreAccessZones.CoreAdminUi,
                CoreAccessZones.CoreAuthorization,
                CoreAccessZones.CoreGeneralFilesWrite,
                CoreAccessZones.CorePublicFilesWrite,
                CoreAccessZones.CoreMeshSettings
            },
            definitions.Select(definition => definition.Id).ToArray());
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task UserAccessEndpoints_ShouldRequireCoreAuthorizationManageAndAllowAdminsToPersistGrants()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = true });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "john", IsGuest = false, IsMain = true });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "claire", IsGuest = false, IsMain = true });

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        UserAuthenticationResponse johnLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "john",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage forbiddenRequest = new(HttpMethod.Get, "/api/core/auth/users/claire/access");
        forbiddenRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnLoginPayload.AccessToken);
        HttpResponseMessage forbiddenResponse = await client.SendAsync(forbiddenRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        UserAuthenticationResponse sarahLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage updateRequest = new(HttpMethod.Put, "/api/core/auth/users/claire/access");
        updateRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sarahLoginPayload.AccessToken);
        updateRequest.Content = JsonContent.Create(new UserAuthorizationUpdateRequest()
        {
            Accesses =
            [
                new UserZoneAccess() { ZoneId = CoreAccessZones.CoreAdminUi, Level = AccessLevel.Read },
                new UserZoneAccess() { ZoneId = CoreAccessZones.CoreAuthorization, Level = AccessLevel.Manage }
            ]
        });

        HttpResponseMessage updateResponse = await client.SendAsync(updateRequest);
        UserAuthorizationProfile updatedProfile = await updateResponse.Content.ReadFromJsonAsync<UserAuthorizationProfile>();

        Assert.AreEqual(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.IsNotNull(updatedProfile);
        Assert.AreEqual(2, updatedProfile.Accesses.Count);

        using HttpRequestMessage getRequest = new(HttpMethod.Get, "/api/core/auth/users/claire/access");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sarahLoginPayload.AccessToken);
        HttpResponseMessage getResponse = await client.SendAsync(getRequest);
        UserAuthorizationProfile reloadedProfile = await getResponse.Content.ReadFromJsonAsync<UserAuthorizationProfile>();

        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.IsNotNull(reloadedProfile);
        Assert.AreEqual("claire", reloadedProfile.UserId);
        Assert.AreEqual(2, reloadedProfile.Accesses.Count);
        Assert.AreEqual(AccessLevel.Read, reloadedProfile.Accesses.Single(access => access.ZoneId == CoreAccessZones.CoreAdminUi).Level);
        Assert.AreEqual(AccessLevel.Manage, reloadedProfile.Accesses.Single(access => access.ZoneId == CoreAccessZones.CoreAuthorization).Level);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task ChangeAdminUser_ShouldTransferAdminExclusivelyToAnotherMainUser()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = true });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "john", IsGuest = false, IsMain = true, IsAdmin = false });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "guest", IsGuest = false, IsMain = false, IsAdmin = false });

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        UserAuthenticationResponse sarahLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "sarah",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage transferRequest = new(HttpMethod.Post, "/api/core/auth/users/john/admin");
        transferRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sarahLoginPayload.AccessToken);
        HttpResponseMessage transferResponse = await client.SendAsync(transferRequest);
        User newAdmin = await transferResponse.Content.ReadFromJsonAsync<User>();

        Assert.AreEqual(HttpStatusCode.OK, transferResponse.StatusCode);
        Assert.IsNotNull(newAdmin);
        Assert.AreEqual("john", newAdmin.Id);
        Assert.IsTrue(newAdmin.IsAdmin);

        User reloadedSarah = await userLogic.GetByIdAsync("sarah");
        User reloadedJohn = await userLogic.GetByIdAsync("john");
        Assert.IsFalse(reloadedSarah.IsAdmin);
        Assert.IsTrue(reloadedJohn.IsAdmin);

        UserAuthenticationResponse johnLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "john",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage invalidTransferRequest = new(HttpMethod.Post, "/api/core/auth/users/guest/admin");
        invalidTransferRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnLoginPayload.AccessToken);
        HttpResponseMessage invalidTransferResponse = await client.SendAsync(invalidTransferRequest);
        ProblemDetails invalidTransferProblem = await invalidTransferResponse.Content.ReadFromJsonAsync<ProblemDetails>();

        Assert.AreEqual(HttpStatusCode.BadRequest, invalidTransferResponse.StatusCode);
        Assert.IsNotNull(invalidTransferProblem);
        Assert.AreEqual("Invalid request", invalidTransferProblem.Title);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task ChangeAdminUser_ShouldRejectNonAdminCallers()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();
        using ProcessEnvironmentVariableScope mongoScope = new ProcessEnvironmentVariableScope("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);

        UserLogic userLogic = new UserLogic();
        await userLogic.SaveAsync(new User() { Id = "sarah", IsGuest = false, IsMain = true, IsAdmin = true });
        await userLogic.SetPasswordAsync("sarah", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "john", IsGuest = false, IsMain = true, IsAdmin = false });
        await userLogic.SetPasswordAsync("john", "P@ssword-42");
        await userLogic.SaveAsync(new User() { Id = "claire", IsGuest = false, IsMain = true, IsAdmin = false });

        await using WebApplication app = CreateApplication();
        await app.StartAsync();
        HttpClient client = app.GetTestClient();

        UserAuthenticationResponse johnLoginPayload = await (await client.PostAsJsonAsync("/api/core/auth/users/login?isInteractive=false", new UserLoginRequest()
        {
            UserId = "john",
            Password = "P@ssword-42"
        })).Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage transferRequest = new(HttpMethod.Post, "/api/core/auth/users/claire/admin");
        transferRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnLoginPayload.AccessToken);
        HttpResponseMessage transferResponse = await client.SendAsync(transferRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, transferResponse.StatusCode);
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