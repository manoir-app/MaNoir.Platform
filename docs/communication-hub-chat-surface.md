# Communication Hub Chat Surface

This document defines the first Communication Hub scope extracted from the legacy monolith.

## Current bootstrap scope

The initial Communication Hub surface is intentionally limited to chat-like exchanges built around three concepts:

- channels
- participants
- messages

The purpose of this first step is to stop treating external interaction as an untyped string feed.

## Legacy surface being replaced

The legacy chat surface currently lives in:

- home-automation/HomeGraph/Home.Graph.Server/Controllers/ChatController.cs
- home-automation/HomeGraph/Home.Graph.Public/Controllers/ChatController.cs
- home-automation/Common/Home.Common/Model/ChatMessage.cs
- home-automation/Common/Home.Graph.Common/ChatHelper.cs

That legacy model stores a message as a single string payload through MessageContent.

## New contract direction

The Communication Hub chat contracts are introduced in MaNoir.CommunicationHub.Contracts and define:

- CommunicationChannel
- CommunicationParticipant
- CommunicationMessage
- CommunicationMessagePart

Messages now support multiple content parts instead of a single text field.

Supported part kinds in the bootstrap contract are:

- PlainText
- Markdown
- HtmlFragment
- StructuredPayload
- Image
- FileReference
- ExternalReference

This keeps the hub neutral regarding rendering while allowing richer interactions than plain text or markdown alone.

## Structured payload conventions

When a message part uses StructuredPayload, the bootstrap contract now defines three explicit JSON conventions:

- card payloads with the media type application/vnd.manoir.communication.card+json
- attachment payloads with the media type application/vnd.manoir.communication.attachment+json
- system event payloads with the media type application/vnd.manoir.communication.system-event+json

These conventions are intentionally simple:

- cards carry a title, summary, markdown body, facts, and optional actions
- attachments carry file identity, mime type, size, URLs, and preview information
- system events carry an event kind, summary, tone, correlation identifiers, and an optional structured detail payload

This gives UIs and agents a stable first-level contract without forcing the hub to own business semantics.

## Persistence bootstrap

The initial persistence bootstrap lives in MaNoir.CommunicationHub as concrete Mongo-backed chat operations.

Current collection split:

- CommunicationChannels
- CommunicationMessages

The persistence layer is intentionally concrete and minimal for now:

- save and load channels
- list channels for one participant
- append messages
- list messages by channel and date
- delete a channel and its messages

## API bootstrap

The first API surface now lives in MaNoir.Core.Api and exposes a minimal authenticated chat API:

- GET /api/core/communication/chat/channels/me
- GET /api/core/communication/chat/channels/{channelId}
- PUT /api/core/communication/chat/channels/{channelId}
- DELETE /api/core/communication/chat/channels/{channelId}
- GET /api/core/communication/chat/channels/{channelId}/messages
- POST /api/core/communication/chat/channels/{channelId}/messages

This API currently follows these rules:

- the current authenticated user is the sender for posted messages
- the current authenticated user is automatically kept in a channel when creating or updating it
- only owners or moderators can update or delete an existing channel
- only channel participants can read a channel or its messages

## First platform producer

The first concrete producer kept inside Platform is a security flow published by Erza.

- channel id: platform:security
- visible sender: agent:erza
- technical source: platform-auth
- transport signals:
	- system.auth.users.login.failed over NATS
	- system.auth.users.password.changed over NATS
- current event kinds:
	- auth.failed-login-threshold-reached
	- auth.password-changed

The current bootstrap updates the failed login state on the authentication side, publishes a technical NATS signal, and lets Erza decide whether the threshold merits a Communication Hub alert.
It also publishes a technical NATS signal after a successful password change, which Erza reformulates as a user-facing security event in the same channel.
The channel is created on demand and currently includes Erza plus the current platform admin when one exists.

## Boundary rule

The Communication Hub owns transport-facing and interaction-facing message structure.

It does not own:

- the final business meaning of a chat message
- domain-specific interpretation of a structured payload
- downstream orchestration triggered by a message

Those concerns remain outside the hub.