using MaNoir.Core.Api;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
    public async Task ChangePassword_ShouldInvalidateThePreviousPasswordAndAcceptTheNewOne()
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
        UserAuthenticationResponse loginPayload = await loginResponse.Content.ReadFromJsonAsync<UserAuthenticationResponse>();

        using HttpRequestMessage changePasswordRequest = new(HttpMethod.Post, "/api/core/auth/users/change-password");
        changePasswordRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", loginPayload.AccessToken);
        changePasswordRequest.Content = JsonContent.Create(new UserChangePasswordRequest()
        {
            CurrentPassword = "P@ssword-42",
            NewPassword = "N3w-P@ssword-42"
        });

        HttpResponseMessage changePasswordResponse = await client.SendAsync(changePasswordRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, changePasswordResponse.StatusCode);

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