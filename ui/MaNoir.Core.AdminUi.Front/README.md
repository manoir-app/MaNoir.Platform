# MaNoir.Core.AdminUi.Front

Primary Admin UI frontend shell for the MaNoir Core host.

## Scope

This module is the future authenticated admin experience served once the local Core instance is already initialized.

For now it intentionally exposes a placeholder waiting page.

## Development

From [ui/package.json](../package.json):

```bash
npm run dev:front
```

The built bundle is meant to be copied into the .NET host under `wwwroot/front`.