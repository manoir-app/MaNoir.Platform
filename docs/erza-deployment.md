# Erza Deployment

Erza now runs as its own process and container.

## Container Images

- CI publishes `ghcr.io/manoir-app/manoir-agents-erza`
- Local development compose builds `apps/MaNoir.Agents.Erza/Dockerfile`

## Local Compose

- `ops/docker-compose.dev.yml` starts MongoDB, NATS, Mosquitto, and Erza.
- Erza does not expose an HTTP port.
- Erza currently depends on MongoDB and NATS.

Example:

```powershell
docker compose -f ops/docker-compose.dev.yml up --build -d
```

## Runtime Variables

Required in practice:

- `MONGODB_CONNECTIONSTRING` or `MONGODB_SERVICE_HOST` + `MONGODB_SERVICE_PORT`
- `NATS_SERVICE_HOST`

Optional:

- `NATS_SERVICE_PORT` default `4222`
- `MANOIR_MESH_ID` default `local`
- `ERZA_AGENT_ID` default `erza`
- `ERZA_DISPLAY_NAME` default `Erza`
- `ERZA_TOPICS` default `users.presence.*,system.mesh.*`
- `ERZA_CAPABILITIES` default `presence,mesh.monitoring`
- `MANOIR_LOCAL_LOCATION_ID` when mobile app usage events must fall back to a known local location

## API Key Note

Erza currently calls shared Core logic directly for agent registry updates.
It does not need `MANOIR_APIKEY` for its in-process register and heartbeat path.

`MANOIR_APIKEY` remains relevant on the HTTP boundary of the Core API when agent registration is performed through HTTP instead of direct shared logic.