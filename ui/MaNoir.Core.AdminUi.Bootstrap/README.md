# MaNoir.Core.AdminUi.Bootstrap

First React bootstrap module for the MaNoir Core admin surface.

## Scope

This module currently owns only the pre-auth and pre-initialization experience:

- first setup gate;
- login screen;
- initial setup screen;
- minimal authenticated landing state.

## Development

From [ui/package.json](../package.json):

```bash
npm install
npm run dev:bootstrap
```

The app calls the Core API through `VITE_CORE_API_BASE_URL`, defaulting to `/api/core`.

For local development, the Vite server now proxies `/api/core` to `http://localhost:5243` by default, which matches the runnable .NET host project `apps/MaNoir.Core.AdminUi`.

If you want another backend target for the dev proxy, set `VITE_CORE_API_PROXY_TARGET`.
If you want the frontend to call a full URL directly instead of using the proxy, set `VITE_CORE_API_BASE_URL`, for example:

```bash
VITE_CORE_API_BASE_URL=http://localhost:5000/api/core npm run dev:bootstrap
```

For the default proxied flow, start the infrastructure dependencies first:

```bash
docker compose -f ../ops/docker-compose.dev.yml up -d
```

Then start the .NET host with:

```bash
dotnet run --project ../apps/MaNoir.Core.AdminUi/MaNoir.Core.AdminUi.csproj --launch-profile http
```