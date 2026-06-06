declare global {
  interface Window {
    __MANOIR_ADMIN_UI_CONFIG__?: {
      routerBasePath?: string;
      publicBasePath?: string;
    };
  }
}

export function getPublicBasePath(): string {
  const runtimePublicBasePath = window.__MANOIR_ADMIN_UI_CONFIG__?.publicBasePath?.trim();
  if (runtimePublicBasePath) {
    return normalizeBasePath(runtimePublicBasePath);
  }

  return '';
}

export function getCoreApiBaseUrl(): string {
  const configuredApiBaseUrl = import.meta.env.VITE_CORE_API_BASE_URL?.trim();
  if (configuredApiBaseUrl) {
    return configuredApiBaseUrl.replace(/\/$/, '');
  }

  return `${getPublicBasePath()}/api/core`;
}

function normalizeBasePath(basePath: string): string {
  const trimmedBasePath = basePath.trim();
  if (!trimmedBasePath || trimmedBasePath === '/') {
    return '';
  }

  return trimmedBasePath.replace(/\/$/, '');
}

export {};