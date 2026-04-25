using MaNoir.Core.Contracts.Models.Setup;
using MaNoir.Core.Contracts.Models.Mesh;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.DataAccess;
using MaNoir.Core.Mesh;
using MaNoir.Core.Users;
using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Core.Setup;

/// <summary>
/// Coordinates the first setup of a local Core instance.
/// </summary>
public sealed class InitialSetupLogic
{
    private static readonly ConcurrentDictionary<string, bool> InitializedStateCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly AutomationMeshLogic _meshLogic;
    private readonly UserLogic _userLogic;

    /// <summary>
    /// Initializes a new instance of the <see cref="InitialSetupLogic"/> class.
    /// </summary>
    public InitialSetupLogic()
    {
        _meshLogic = new AutomationMeshLogic();
        _userLogic = new UserLogic();
    }

    /// <summary>
    /// Invalidates the cached initialization state for the current MongoDB binding.
    /// </summary>
    public static void InvalidateCachedStatus()
    {
        InitializedStateCache.TryRemove(ResolveCacheKey(), out _);
    }

    /// <summary>
    /// Invalidates the cached initialization state for one explicit MongoDB connection string.
    /// </summary>
    public static void InvalidateCachedStatus(string connectionString)
    {
        InitializedStateCache.TryRemove(ResolveCacheKey(connectionString), out _);
    }

    /// <summary>
    /// Gets whether the first setup can still be executed.
    /// </summary>
    public async Task<InitialSetupStatus> GetStatusAsync(CancellationToken cancellationToken = default)
    {
        string cacheKey = ResolveCacheKey();
        if (InitializedStateCache.TryGetValue(cacheKey, out bool isInitialized) && isInitialized)
            return CreateInitializedStatus();

        bool hasMesh = await _meshLogic.GetLocalAsync(cancellationToken) != null;
        bool hasUsers = (await _userLogic.GetAllAsync(cancellationToken)).Count > 0;

        InitialSetupStatus status = new InitialSetupStatus()
        {
            HasMesh = hasMesh,
            HasUsers = hasUsers,
            CanInitialize = !hasMesh && !hasUsers
        };

        if (hasMesh && hasUsers)
            InitializedStateCache[cacheKey] = true;

        return status;
    }

    /// <summary>
    /// Initializes the local mesh and the first master admin user.
    /// </summary>
    public async Task<InitialSetupResponse> InitializeAsync(InitialSetupRequest request, string graphApiBaseUri, string machineName, CancellationToken cancellationToken = default)
    {
        string cacheKey = ResolveCacheKey();
        InitialSetupStatus status = await GetStatusAsync(cancellationToken);
        if (!status.CanInitialize)
            throw new InitialSetupUnavailableException();

        string normalizedLanguageId;
        string normalizedTimeZoneId;
        string normalizedCountryId;
        ValidateRequest(request, graphApiBaseUri, machineName, out normalizedLanguageId, out normalizedTimeZoneId, out normalizedCountryId);

        bool meshCreated = false;
        bool userCreated = false;
        string normalizedAdminUserId = UserLogic.NormalizeUserId(request.AdminUserId);

        try
        {
            AutomationMesh mesh = AutomationMeshLogic.CreateLocalMesh(machineName, graphApiBaseUri);
            if (normalizedLanguageId != null || normalizedTimeZoneId != null)
                AutomationMeshLogic.ApplySettings(mesh, normalizedLanguageId, normalizedTimeZoneId);

            if (normalizedCountryId != null)
                AutomationMeshLogic.ApplyCountryId(mesh, normalizedCountryId);

            await _meshLogic.SaveAsync(mesh, cancellationToken);
            meshCreated = true;

            User createdUser = await _userLogic.UpsertUserAsync(normalizedAdminUserId, new User()
            {
                IsMain = true,
                FirstName = request.AdminFirstName,
                Name = request.AdminName,
                CommonName = request.AdminCommonName,
                MainEmail = request.AdminEmail
            }, cancellationToken);
            userCreated = createdUser != null;

            await _userLogic.SetPasswordAsync(normalizedAdminUserId, request.AdminPassword, cancellationToken);
            InitializedStateCache[cacheKey] = true;

            return new InitialSetupResponse()
            {
                Mesh = await _meshLogic.GetLocalAsync(cancellationToken),
                User = CreateUserProjection(await _userLogic.GetByIdAsync(normalizedAdminUserId, cancellationToken))
            };
        }
        catch
        {
            InitializedStateCache.TryRemove(cacheKey, out _);

            if (userCreated)
                await _userLogic.DeleteOtherUserAsync(normalizedAdminUserId, cancellationToken);

            if (meshCreated)
                await _meshLogic.DeleteLocalAsync(cancellationToken);

            throw;
        }
    }

    private static void ValidateRequest(InitialSetupRequest request, string graphApiBaseUri, string machineName, out string normalizedLanguageId, out string normalizedTimeZoneId, out string normalizedCountryId)
    {
        normalizedLanguageId = null;
        normalizedTimeZoneId = null;
        normalizedCountryId = null;

        if (request == null)
            throw new ArgumentNullException(nameof(request));

        if (UserLogic.NormalizeUserId(request.AdminUserId) == null)
            throw new ArgumentException("The master admin user identifier cannot be empty.", nameof(request));

        if (string.IsNullOrWhiteSpace(request.AdminPassword))
            throw new ArgumentException("The master admin password cannot be empty.", nameof(request));

        if (string.IsNullOrWhiteSpace(machineName))
            throw new ArgumentException("The machine name cannot be empty.", nameof(machineName));

        if (!Uri.TryCreate(graphApiBaseUri, UriKind.Absolute, out _))
            throw new ArgumentException("The graph API base URI must be absolute.", nameof(graphApiBaseUri));

        if (!string.IsNullOrWhiteSpace(request.LanguageId))
        {
            normalizedLanguageId = AutomationMeshLogic.NormalizeLanguageId(request.LanguageId);
            if (normalizedLanguageId == null)
                throw new ArgumentException("The mesh language identifier is invalid.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.TimeZoneId))
        {
            normalizedTimeZoneId = AutomationMeshLogic.NormalizeIanaTimeZoneId(request.TimeZoneId);
            if (normalizedTimeZoneId == null)
                throw new ArgumentException("The mesh time zone identifier is invalid.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.CountryId))
        {
            normalizedCountryId = AutomationMeshLogic.NormalizeCountryId(request.CountryId);
            if (normalizedCountryId == null)
                throw new ArgumentException("The mesh country identifier is invalid.", nameof(request));
        }
    }

    private static User CreateUserProjection(User user)
    {
        if (user == null)
            return null;

        User projectedUser = new User()
        {
            Id = user.Id,
            IsGuest = user.IsGuest,
            IsMain = user.IsMain,
            Name = user.Name,
            FirstName = user.FirstName,
            CommonName = user.CommonName,
            SsmlTaggedName = user.SsmlTaggedName,
            MainEmail = user.MainEmail,
            MainPhoneNumber = user.MainPhoneNumber,
            Avatar = user.Avatar
        };

        UserLogic.MinimizeData(projectedUser);
        return projectedUser;
    }

    private static InitialSetupStatus CreateInitializedStatus()
    {
        return new InitialSetupStatus()
        {
            CanInitialize = false,
            HasMesh = true,
            HasUsers = true
        };
    }

    private static string ResolveCacheKey()
    {
        string connectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTIONSTRING");
        return ResolveCacheKey(connectionString);
    }

    private static string ResolveCacheKey(string connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString))
            return string.Concat(MongoDbHelper.DefaultDatabaseName, "|", connectionString.Trim());

        string host = Environment.GetEnvironmentVariable("MONGODB_SERVICE_HOST");
        string portText = Environment.GetEnvironmentVariable("MONGODB_SERVICE_PORT");
        if (!string.IsNullOrWhiteSpace(host) && !string.IsNullOrWhiteSpace(portText))
            return string.Concat(MongoDbHelper.DefaultDatabaseName, "|mongodb://", host.Trim(), ":", portText.Trim());

        return string.Concat(MongoDbHelper.DefaultDatabaseName, "|default");
    }
}