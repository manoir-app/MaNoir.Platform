using MaNoir.CommunicationHub.Chat;
using MaNoir.CommunicationHub.Contracts.Models.Chat;
using MaNoir.Core.Contracts.Models.Users;
using MaNoir.Core.Users;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MaNoir.Agents.Erza;

/// <summary>
/// Publishes Erza security messages into the Communication Hub from technical platform signals.
/// </summary>
public sealed class ErzaSecurityCommunicationPublisher
{
    /// <summary>
    /// Gets the canonical identifier of the platform security channel.
    /// </summary>
    public const string SecurityChannelId = "platform:security";

    /// <summary>
    /// Gets the canonical participant identifier used by Erza in platform security messages.
    /// </summary>
    public const string ErzaParticipantId = "agent:erza";

    /// <summary>
    /// Gets the event kind emitted when repeated failed logins cross the alert threshold.
    /// </summary>
    public const string FailedLoginThresholdEventKind = "auth.failed-login-threshold-reached";

    /// <summary>
    /// Gets the event kind emitted when a user changes their password.
    /// </summary>
    public const string PasswordChangedEventKind = "auth.password-changed";

    private const int FailedLoginThreshold = 5;
    private static readonly JsonSerializerOptions CamelCaseJson = new JsonSerializerOptions()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly CommunicationChatLogic _chatLogic;
    private readonly UserLogic _userLogic;
    private readonly UserFailedLoginStateTracker _tracker;

    /// <summary>
    /// Initializes a new instance of the <see cref="ErzaSecurityCommunicationPublisher"/> class.
    /// </summary>
    public ErzaSecurityCommunicationPublisher()
    {
        _chatLogic = new CommunicationChatLogic();
        _userLogic = new UserLogic();
        _tracker = new UserFailedLoginStateTracker();
    }

    /// <summary>
    /// Processes one failed login signal and publishes a Communication Hub alert when the threshold is reached.
    /// </summary>
    public async Task ProcessFailedLoginAsync(string userId, CancellationToken cancellationToken = default)
    {
        UserFailedLoginState state = await _tracker.GetAsync(userId, cancellationToken);
        if (state == null || state.FailedCount < FailedLoginThreshold)
            return;

        if (state.LastAlertSentAtUtc.HasValue && state.LastAlertSentAtUtc.Value >= state.WindowStartedAtUtc)
            return;

        await EnsureSecurityChannelAsync(cancellationToken);

        CommunicationSystemEventPayload payload = new CommunicationSystemEventPayload()
        {
            EventKind = FailedLoginThresholdEventKind,
            Summary = $"Trop de tentatives de connexion pour {state.UserId}",
            Tone = CommunicationSystemEventTone.Warning,
            CorrelationId = $"failed-login:{state.UserId}:{state.LastFailedAtUtc:yyyyMMddHHmm}",
            RelatedEntityKind = "user",
            RelatedEntityId = state.UserId,
            DetailJson = JsonSerializer.Serialize(new
            {
                threshold = FailedLoginThreshold,
                failedCount = state.FailedCount,
                windowStartedAtUtc = state.WindowStartedAtUtc,
                lastFailedAtUtc = state.LastFailedAtUtc,
                remoteAddress = state.LastRemoteAddress,
                userAgent = state.LastUserAgent,
                speaker = ErzaParticipantId,
                sourceModule = "platform-auth"
            }, CamelCaseJson)
        };

        await _chatLogic.AppendMessageAsync(new CommunicationMessage()
        {
            ChannelId = SecurityChannelId,
            SenderParticipantId = ErzaParticipantId,
            Kind = CommunicationMessageKind.Event,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceModule"] = "platform-auth",
                ["sourceKind"] = "security",
                ["speakerAgent"] = ErzaParticipantId,
            },
            Parts =
            [
                new CommunicationMessagePart()
                {
                    Kind = CommunicationMessagePartKind.StructuredPayload,
                    MimeType = CommunicationPayloadMimeTypes.SystemEvent,
                    PayloadJson = JsonSerializer.Serialize(payload, CamelCaseJson)
                }
            ]
        }, cancellationToken);

        await _tracker.MarkAlertSentAsync(state.UserId, DateTimeOffset.UtcNow, cancellationToken);
    }

    /// <summary>
    /// Publishes a Communication Hub event for a successful password change.
    /// </summary>
    public async Task ProcessPasswordChangedAsync(string userId, DateTimeOffset changedAtUtc, string remoteAddress, string userAgent, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return;

        await EnsureSecurityChannelAsync(cancellationToken);

        CommunicationSystemEventPayload payload = new CommunicationSystemEventPayload()
        {
            EventKind = PasswordChangedEventKind,
            Summary = $"Mot de passe mis a jour pour {userId}",
            Tone = CommunicationSystemEventTone.Success,
            CorrelationId = $"password-changed:{userId}:{changedAtUtc:yyyyMMddHHmmss}",
            RelatedEntityKind = "user",
            RelatedEntityId = userId,
            DetailJson = JsonSerializer.Serialize(new
            {
                changedAtUtc,
                remoteAddress,
                userAgent,
                speaker = ErzaParticipantId,
                sourceModule = "platform-auth"
            }, CamelCaseJson)
        };

        await _chatLogic.AppendMessageAsync(new CommunicationMessage()
        {
            ChannelId = SecurityChannelId,
            SenderParticipantId = ErzaParticipantId,
            Kind = CommunicationMessageKind.Event,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["sourceModule"] = "platform-auth",
                ["sourceKind"] = "security",
                ["speakerAgent"] = ErzaParticipantId,
            },
            Parts =
            [
                new CommunicationMessagePart()
                {
                    Kind = CommunicationMessagePartKind.StructuredPayload,
                    MimeType = CommunicationPayloadMimeTypes.SystemEvent,
                    PayloadJson = JsonSerializer.Serialize(payload, CamelCaseJson)
                }
            ]
        }, cancellationToken);
    }

    private async Task EnsureSecurityChannelAsync(CancellationToken cancellationToken)
    {
        List<CommunicationParticipant> participants =
        [
            new CommunicationParticipant()
            {
                Id = ErzaParticipantId,
                DisplayName = "Erza",
                Kind = CommunicationParticipantKind.Agent,
                Role = CommunicationParticipantRole.System,
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["domain"] = "security"
                }
            }
        ];

        User adminUser = await _userLogic.GetAdminUserAsync(cancellationToken);
        if (adminUser != null && !string.IsNullOrWhiteSpace(adminUser.Id))
        {
            participants.Add(new CommunicationParticipant()
            {
                Id = adminUser.Id,
                DisplayName = adminUser.CommonName ?? adminUser.FirstName ?? adminUser.Id,
                Kind = CommunicationParticipantKind.User,
                Role = CommunicationParticipantRole.Owner,
                Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    ["audience"] = "platform-admin"
                }
            });
        }

        await _chatLogic.UpsertChannelAsync(new CommunicationChannel()
        {
            Id = SecurityChannelId,
            Label = "Securite platform",
            Kind = CommunicationChannelKind.System,
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["module"] = "platform-auth",
                ["speakerAgent"] = ErzaParticipantId,
            },
            Participants = participants
        }, cancellationToken);
    }
}