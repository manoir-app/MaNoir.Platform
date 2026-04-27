using MaNoir.Core.Api;
using MaNoir.Core.Authorization;
using MaNoir.Core.Contracts.Models.Authorization;
using MaNoir.Core.Contracts.Models.Files;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Files;
using MaNoir.Core.FunctionalTests.Infrastructure;
using MaNoir.Core.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MaNoir.Core.FunctionalTests.Api;

[TestClass]
public sealed class FilesApiTests
{
    [TestMethod]
    [TestCategory("Functional")]
    public async Task GeneralFiles_ShouldRequireAuthenticationForReadAndContributeAccessForWrite()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        string tempFolder = Path.Combine(Path.GetTempPath(), "manoir-core-files-tests", Guid.NewGuid().ToString("N"));

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope fileScope = new("MANOIR_FILE_STORAGE_FOLDER", tempFolder);

        await SeedUsersAsync();
        await new AuthorizationLogic().ReplaceUserAuthorizationAsync("claire",
        [
            new UserZoneAccess() { ZoneId = CoreAccessZones.CoreGeneralFilesWrite, Level = AccessLevel.Contribute }
        ]);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string claireToken = await LoginAsync(client, "claire");
        string johnToken = await LoginAsync(client, "john");
        byte[] payload = Encoding.UTF8.GetBytes("hello-general-file");
        string sha256 = Convert.ToHexString(SHA256.HashData(payload));

        using HttpRequestMessage forbiddenPutRequest = new(HttpMethod.Put, "/api/core/files/general/users/avatars/sarah/avatar.txt")
        {
            Content = new ByteArrayContent(payload)
        };
        forbiddenPutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);
        HttpResponseMessage forbiddenPutResponse = await client.SendAsync(forbiddenPutRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenPutResponse.StatusCode);

        using HttpRequestMessage putRequest = new(HttpMethod.Put, $"/api/core/files/general/users/avatars/sarah/avatar.txt?contentType=text/plain&sha256={sha256}&length={payload.Length}")
        {
            Content = new ByteArrayContent(payload)
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claireToken);
        putRequest.Content.Headers.ContentType = new MediaTypeHeaderValue("text/plain");

        HttpResponseMessage putResponse = await client.SendAsync(putRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "general", "users", "avatars", "sarah", "avatar.txt")));

        HttpResponseMessage anonymousGetResponse = await client.GetAsync("/api/core/files/general/users/avatars/sarah/avatar.txt");
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousGetResponse.StatusCode);

        using HttpRequestMessage getRequest = new(HttpMethod.Get, "/api/core/files/general/users/avatars/sarah/avatar.txt");
        getRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage getResponse = await client.SendAsync(getRequest);
        byte[] downloadedPayload = await getResponse.Content.ReadAsByteArrayAsync();

        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.AreEqual("text/plain", getResponse.Content.Headers.ContentType?.MediaType);
        CollectionAssert.AreEqual(payload, downloadedPayload);

        using HttpRequestMessage metadataRequest = new(HttpMethod.Get, "/api/core/files/general/users/metadata/avatars/sarah/avatar.txt");
        metadataRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);
        HttpResponseMessage metadataResponse = await client.SendAsync(metadataRequest);
        StoredFileMetadata metadata = await metadataResponse.Content.ReadFromJsonAsync<StoredFileMetadata>();

        Assert.AreEqual(HttpStatusCode.OK, metadataResponse.StatusCode);
        Assert.IsNotNull(metadata);
        Assert.AreEqual("text/plain", metadata.ContentType);
        Assert.AreEqual(payload.Length, metadata.Length);
        Assert.AreEqual(sha256, metadata.Sha256);

        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PublicFiles_ShouldAllowAnonymousReadAndRequireContributeAccessForWrite()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        string tempFolder = Path.Combine(Path.GetTempPath(), "manoir-core-files-tests", Guid.NewGuid().ToString("N"));

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope fileScope = new("MANOIR_FILE_STORAGE_FOLDER", tempFolder);

        await SeedUsersAsync();
        await new AuthorizationLogic().ReplaceUserAuthorizationAsync("claire",
        [
            new UserZoneAccess() { ZoneId = CoreAccessZones.CorePublicFilesWrite, Level = AccessLevel.Contribute }
        ]);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string claireToken = await LoginAsync(client, "claire");
        string johnToken = await LoginAsync(client, "john");
        byte[] payload = Encoding.UTF8.GetBytes("hello-public-file");

        using HttpRequestMessage forbiddenPutRequest = new(HttpMethod.Put, "/api/core/files/public/core-assets/banner.txt")
        {
            Content = new ByteArrayContent(payload)
        };
        forbiddenPutRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);
        HttpResponseMessage forbiddenPutResponse = await client.SendAsync(forbiddenPutRequest);

        Assert.AreEqual(HttpStatusCode.Forbidden, forbiddenPutResponse.StatusCode);

        using HttpRequestMessage putRequest = new(HttpMethod.Put, "/api/core/files/public/core-assets/banner.txt?contentType=text/plain")
        {
            Content = new ByteArrayContent(payload)
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claireToken);

        HttpResponseMessage putResponse = await client.SendAsync(putRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "public", "core-assets", "banner.txt")));

        HttpResponseMessage getResponse = await client.GetAsync("/api/core/files/public/core-assets/banner.txt");
        byte[] downloadedPayload = await getResponse.Content.ReadAsByteArrayAsync();

        HttpResponseMessage metadataResponse = await client.GetAsync("/api/core/files/public/core-assets/metadata/banner.txt");
        StoredFileMetadata metadata = await metadataResponse.Content.ReadFromJsonAsync<StoredFileMetadata>();

        Assert.AreEqual(HttpStatusCode.OK, getResponse.StatusCode);
        CollectionAssert.AreEqual(payload, downloadedPayload);
        Assert.AreEqual(HttpStatusCode.OK, metadataResponse.StatusCode);
        Assert.AreEqual("text/plain", metadata.ContentType);

        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task CurrentUserFiles_ShouldOnlyRequireAuthenticationAndStayScopedToTheCaller()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        string tempFolder = Path.Combine(Path.GetTempPath(), "manoir-core-files-tests", Guid.NewGuid().ToString("N"));

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope fileScope = new("MANOIR_FILE_STORAGE_FOLDER", tempFolder);

        await SeedUsersAsync();

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string johnToken = await LoginAsync(client, "john");
        string sarahToken = await LoginAsync(client, "sarah");
        byte[] payload = Encoding.UTF8.GetBytes("hello-user-file");
        string sha256 = Convert.ToHexString(SHA256.HashData(payload));

        using HttpRequestMessage putRequest = new(HttpMethod.Put, $"/api/core/files/users/me/preferences/avatar.txt?sha256={sha256}&length={payload.Length}")
        {
            Content = new ByteArrayContent(payload)
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage putResponse = await client.SendAsync(putRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, putResponse.StatusCode);
        Assert.IsTrue(File.Exists(Path.Combine(tempFolder, "users", "john", "preferences", "avatar.txt")));

        HttpResponseMessage anonymousGetResponse = await client.GetAsync("/api/core/files/users/me/preferences/avatar.txt");
        Assert.AreEqual(HttpStatusCode.Unauthorized, anonymousGetResponse.StatusCode);

        using HttpRequestMessage johnGetRequest = new(HttpMethod.Get, "/api/core/files/users/me/preferences/avatar.txt");
        johnGetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);
        HttpResponseMessage johnGetResponse = await client.SendAsync(johnGetRequest);
        byte[] downloadedPayload = await johnGetResponse.Content.ReadAsByteArrayAsync();

        Assert.AreEqual(HttpStatusCode.OK, johnGetResponse.StatusCode);
        CollectionAssert.AreEqual(payload, downloadedPayload);

        using HttpRequestMessage metadataRequest = new(HttpMethod.Get, "/api/core/files/users/me/preferences/metadata/avatar.txt");
        metadataRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);
        HttpResponseMessage metadataResponse = await client.SendAsync(metadataRequest);
        StoredFileMetadata metadata = await metadataResponse.Content.ReadFromJsonAsync<StoredFileMetadata>();

        Assert.AreEqual(HttpStatusCode.OK, metadataResponse.StatusCode);
        Assert.AreEqual(payload.Length, metadata.Length);
        Assert.AreEqual(sha256, metadata.Sha256);

        using HttpRequestMessage sarahGetRequest = new(HttpMethod.Get, "/api/core/files/users/me/preferences/avatar.txt");
        sarahGetRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", sarahToken);
        HttpResponseMessage sarahGetResponse = await client.SendAsync(sarahGetRequest);

        Assert.AreEqual(HttpStatusCode.NotFound, sarahGetResponse.StatusCode);

        using HttpRequestMessage deleteRequest = new(HttpMethod.Delete, "/api/core/files/users/me/preferences/avatar.txt");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);

        HttpResponseMessage deleteResponse = await client.SendAsync(deleteRequest);

        Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        Assert.IsFalse(File.Exists(Path.Combine(tempFolder, "users", "john", "preferences", "avatar.txt")));

        using HttpRequestMessage getAfterDeleteRequest = new(HttpMethod.Get, "/api/core/files/users/me/preferences/avatar.txt");
        getAfterDeleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", johnToken);
        HttpResponseMessage getAfterDeleteResponse = await client.SendAsync(getAfterDeleteRequest);

        Assert.AreEqual(HttpStatusCode.NotFound, getAfterDeleteResponse.StatusCode);

        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
    }

    [TestMethod]
    [TestCategory("Functional")]
    public void FileStorageHelper_ShouldRejectPathTraversal()
    {
        Assert.IsNull(FileStorageHelper.GetGeneralFilePath("users", "avatars/../escape.txt"));
        Assert.IsNull(FileStorageHelper.GetPublicFilePath("users", "avatars\\..\\escape.txt"));
        Assert.IsNull(FileStorageHelper.GetUserFilePath("john", "bad:scope", "avatars/sarah/avatar.txt"));
    }

    [TestMethod]
    [TestCategory("Functional")]
    public async Task PutGeneralFile_ShouldRejectMismatchedSha256()
    {
        await using MongoDbFunctionalTestHost mongoHost = new MongoDbFunctionalTestHost();
        await mongoHost.StartAsync();

        string tempFolder = Path.Combine(Path.GetTempPath(), "manoir-core-files-tests", Guid.NewGuid().ToString("N"));

        using ProcessEnvironmentVariableScope mongoScope = new("MONGODB_CONNECTIONSTRING", mongoHost.ConnectionString);
        using ProcessEnvironmentVariableScope fileScope = new("MANOIR_FILE_STORAGE_FOLDER", tempFolder);

        await SeedUsersAsync();
        await new AuthorizationLogic().ReplaceUserAuthorizationAsync("claire",
        [
            new UserZoneAccess() { ZoneId = CoreAccessZones.CoreGeneralFilesWrite, Level = AccessLevel.Contribute }
        ]);

        await using WebApplication app = CreateApplication();
        await app.StartAsync();

        HttpClient client = app.GetTestClient();
        string claireToken = await LoginAsync(client, "claire");

        using HttpRequestMessage putRequest = new(HttpMethod.Put, "/api/core/files/general/users/avatars/sarah/avatar.txt?sha256=BADHASH")
        {
            Content = new ByteArrayContent(Encoding.UTF8.GetBytes("hello-general-file"))
        };
        putRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", claireToken);

        HttpResponseMessage putResponse = await client.SendAsync(putRequest);

        Assert.AreEqual(HttpStatusCode.BadRequest, putResponse.StatusCode);
        Assert.IsFalse(File.Exists(Path.Combine(tempFolder, "general", "users", "avatars", "sarah", "avatar.txt")));

        if (Directory.Exists(tempFolder))
            Directory.Delete(tempFolder, true);
    }

    private static async Task SeedUsersAsync()
    {
        UserLogic userLogic = new();
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