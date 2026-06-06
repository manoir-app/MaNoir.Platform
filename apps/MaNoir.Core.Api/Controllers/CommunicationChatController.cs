using MaNoir.CommunicationHub.Chat;
using MaNoir.CommunicationHub.Contracts.Models.Chat;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MaNoir.Core.Api.Controllers;

[ApiController]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
[Route("api/core/communication/chat")]
public sealed class CommunicationChatController : ControllerBase
{
    [HttpGet("channels/me")]
    public async Task<ActionResult<List<CommunicationChannelResponse>>> GetMyChannels()
    {
        string currentUserId = EnsureCurrentUserId();
        List<CommunicationChannel> channels = await new CommunicationChatLogic().GetChannelsForParticipantAsync(currentUserId, HttpContext.RequestAborted);
        return Ok(channels.Select(MapChannel).ToList());
    }

    [HttpGet("channels/{channelId}")]
    public async Task<ActionResult<CommunicationChannelResponse>> GetChannel(string channelId)
    {
        string currentUserId = EnsureCurrentUserId();
        string normalizedChannelId = CommunicationChatLogic.NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return CreateInvalidRequestResponse("channelId", "The channel identifier is required.");

        CommunicationChannel channel = await new CommunicationChatLogic().GetChannelByIdAsync(normalizedChannelId, HttpContext.RequestAborted);
        if (channel == null || !IsParticipant(channel, currentUserId))
            return NotFound();

        return Ok(MapChannel(channel));
    }

    [HttpPut("channels/{channelId}")]
    public async Task<ActionResult<CommunicationChannelResponse>> PutChannel(string channelId, [FromBody] CommunicationChannelUpsertRequest request)
    {
        string currentUserId = EnsureCurrentUserId();
        string normalizedChannelId = CommunicationChatLogic.NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return CreateInvalidRequestResponse("channelId", "The channel identifier is required.");

        CommunicationChatLogic logic = new CommunicationChatLogic();
        CommunicationChannel existingChannel = await logic.GetChannelByIdAsync(normalizedChannelId, HttpContext.RequestAborted);

        if (existingChannel != null)
            EnsureCanManageChannel(existingChannel, currentUserId);

        CommunicationChannel channel = MapChannel(request, normalizedChannelId, existingChannel, currentUserId);
        CommunicationChannel storedChannel = await logic.UpsertChannelAsync(channel, HttpContext.RequestAborted);
        return Ok(MapChannel(storedChannel));
    }

    [HttpDelete("channels/{channelId}")]
    public async Task<IActionResult> DeleteChannel(string channelId)
    {
        string currentUserId = EnsureCurrentUserId();
        string normalizedChannelId = CommunicationChatLogic.NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return CreateInvalidRequestResponse("channelId", "The channel identifier is required.");

        CommunicationChatLogic logic = new CommunicationChatLogic();
        CommunicationChannel existingChannel = await logic.GetChannelByIdAsync(normalizedChannelId, HttpContext.RequestAborted);
        if (existingChannel == null || !IsParticipant(existingChannel, currentUserId))
            return NotFound();

        EnsureCanManageChannel(existingChannel, currentUserId);

        bool deleted = await logic.DeleteChannelAsync(normalizedChannelId, HttpContext.RequestAborted);
        return deleted ? NoContent() : NotFound();
    }

    [HttpGet("channels/{channelId}/messages")]
    public async Task<ActionResult<List<CommunicationMessageResponse>>> GetMessages(string channelId, DateTimeOffset? since = null, int limit = 200)
    {
        string currentUserId = EnsureCurrentUserId();
        string normalizedChannelId = CommunicationChatLogic.NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return CreateInvalidRequestResponse("channelId", "The channel identifier is required.");

        CommunicationChatLogic logic = new CommunicationChatLogic();
        CommunicationChannel channel = await logic.GetChannelByIdAsync(normalizedChannelId, HttpContext.RequestAborted);
        if (channel == null || !IsParticipant(channel, currentUserId))
            return NotFound();

        List<CommunicationMessage> messages = await logic.GetMessagesAsync(normalizedChannelId, since, limit, HttpContext.RequestAborted);
        return Ok(messages.Select(MapMessage).ToList());
    }

    [HttpPost("channels/{channelId}/messages")]
    public async Task<ActionResult<CommunicationMessageResponse>> PostMessage(string channelId, [FromBody] CommunicationMessageAppendRequest request)
    {
        string currentUserId = EnsureCurrentUserId();
        string normalizedChannelId = CommunicationChatLogic.NormalizeChannelId(channelId);
        if (normalizedChannelId == null)
            return CreateInvalidRequestResponse("channelId", "The channel identifier is required.");

        CommunicationMessage message = new CommunicationMessage()
        {
            ChannelId = normalizedChannelId,
            SenderParticipantId = currentUserId,
            Kind = request?.Kind ?? CommunicationMessageKind.Standard,
            PreviewText = request?.PreviewText,
            Metadata = request?.Metadata ?? new Dictionary<string, string>(),
            Parts = request?.Parts?.Select(MapMessagePart).ToList() ?? new List<CommunicationMessagePart>()
        };

        CommunicationMessage storedMessage = await new CommunicationChatLogic().AppendMessageAsync(message, HttpContext.RequestAborted);
        return Ok(MapMessage(storedMessage));
    }

    private string EnsureCurrentUserId()
    {
        string currentUserId = CoreApiUserContext.GetUserId(this);
        if (string.IsNullOrWhiteSpace(currentUserId))
            throw new UnauthorizedAccessException("An authenticated user is required.");

        return currentUserId;
    }

    private static bool IsParticipant(CommunicationChannel channel, string participantId)
    {
        return channel?.Participants != null
            && channel.Participants.Any(participant => participant != null && participant.Id == participantId);
    }

    private static void EnsureCanManageChannel(CommunicationChannel channel, string participantId)
    {
        CommunicationParticipant participant = channel?.Participants?.FirstOrDefault(item => item != null && item.Id == participantId);
        if (participant == null)
            throw new UnauthorizedAccessException("Only channel participants can manage the channel.");

        if (participant.Role != CommunicationParticipantRole.Owner && participant.Role != CommunicationParticipantRole.Moderator)
            throw new UnauthorizedAccessException("Only channel owners or moderators can manage the channel.");
    }

    private static CommunicationChannel MapChannel(CommunicationChannelUpsertRequest request, string channelId, CommunicationChannel existingChannel, string currentUserId)
    {
        List<CommunicationParticipant> participants = request?.Participants?
            .Where(participant => participant != null)
            .Select(participant => new CommunicationParticipant()
            {
                Id = participant.ParticipantId,
                DisplayName = participant.DisplayName,
                Kind = participant.Kind,
                Role = participant.Role,
                Metadata = participant.Metadata ?? new Dictionary<string, string>()
            })
            .ToList() ?? new List<CommunicationParticipant>();

        CommunicationParticipant existingCurrentUser = existingChannel?.Participants?.FirstOrDefault(participant => participant != null && participant.Id == currentUserId);
        if (!participants.Any(participant => CommunicationChatLogic.NormalizeParticipantId(participant.Id) == currentUserId))
        {
            participants.Add(new CommunicationParticipant()
            {
                Id = currentUserId,
                DisplayName = existingCurrentUser?.DisplayName ?? currentUserId,
                Kind = existingCurrentUser?.Kind ?? CommunicationParticipantKind.User,
                Role = existingCurrentUser?.Role ?? CommunicationParticipantRole.Owner,
                Metadata = existingCurrentUser?.Metadata ?? new Dictionary<string, string>()
            });
        }

        return new CommunicationChannel()
        {
            Id = channelId,
            Label = request?.Label,
            Kind = request?.Kind ?? CommunicationChannelKind.Group,
            Participants = participants,
            Metadata = request?.Metadata ?? new Dictionary<string, string>()
        };
    }

    private static CommunicationMessagePart MapMessagePart(CommunicationMessagePartRequest part)
    {
        return new CommunicationMessagePart()
        {
            Kind = part.Kind,
            MimeType = part.MimeType,
            Text = part.Text,
            Url = part.Url,
            FileName = part.FileName,
            PayloadJson = part.PayloadJson
        };
    }

    private static CommunicationChannelResponse MapChannel(CommunicationChannel channel)
    {
        return new CommunicationChannelResponse()
        {
            Id = channel.Id,
            Label = channel.Label,
            Kind = channel.Kind,
            Metadata = channel.Metadata ?? new Dictionary<string, string>(),
            Participants = channel.Participants?.Select(participant => new CommunicationParticipantResponse()
            {
                ParticipantId = participant.Id,
                DisplayName = participant.DisplayName,
                Kind = participant.Kind,
                Role = participant.Role,
                Metadata = participant.Metadata ?? new Dictionary<string, string>()
            }).ToList() ?? []
        };
    }

    private static CommunicationMessageResponse MapMessage(CommunicationMessage message)
    {
        return new CommunicationMessageResponse()
        {
            Id = message.Id,
            ChannelId = message.ChannelId,
            SenderParticipantId = message.SenderParticipantId,
            Kind = message.Kind,
            SentAt = message.SentAt.ToString("O"),
            PreviewText = message.PreviewText,
            Metadata = message.Metadata ?? new Dictionary<string, string>(),
            Parts = message.Parts?.Select(part => new CommunicationMessagePartResponse()
            {
                Kind = part.Kind,
                MimeType = part.MimeType,
                Text = part.Text,
                Url = part.Url,
                FileName = part.FileName,
                PayloadJson = part.PayloadJson
            }).ToList() ?? []
        };
    }

    private BadRequestObjectResult CreateInvalidRequestResponse(string fieldName, string message)
    {
        return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>()
        {
            [fieldName] = [message]
        }));
    }
}