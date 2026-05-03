import { useTranslation } from 'react-i18next';
import type { CoreServerHealthInfo, UserModel } from '../lib/api';
import { useAuthSessionStore } from '../stores/authSessionStore';

function formatUptime(uptimeSeconds: number) {
  const totalSeconds = Math.max(0, Math.floor(uptimeSeconds));
  const days = Math.floor(totalSeconds / 86400);
  const hours = Math.floor((totalSeconds % 86400) / 3600);
  const minutes = Math.floor((totalSeconds % 3600) / 60);

  if (days > 0) {
    return `${days}j ${hours}h`;
  }

  if (hours > 0) {
    return `${hours}h ${minutes}m`;
  }

  return `${Math.max(1, minutes)}m`;
}

function formatHeaderDate(date: Date, locale: string) {
  return new Intl.DateTimeFormat(locale, {
    weekday: 'long',
    day: 'numeric',
    month: 'long',
    year: 'numeric',
  }).format(date);
}

export function useAuthSessionViewModel() {
  const { i18n } = useTranslation();
  const currentUser = useAuthSessionStore((state) => state.currentUser) as UserModel | null;
  const serverInfo = useAuthSessionStore((state) => state.serverInfo) as CoreServerHealthInfo | null;
  const serverInfoState = useAuthSessionStore((state) => state.serverInfoState);
  const isSubmitting = useAuthSessionStore((state) => state.isSubmitting);
  const isRestoringSession = useAuthSessionStore((state) => state.isRestoringSession);
  const hasRestoredSession = useAuthSessionStore((state) => state.hasRestoredSession);
  const errorMessage = useAuthSessionStore((state) => state.errorMessage);
  const restoreSession = useAuthSessionStore((state) => state.restoreSession);
  const login = useAuthSessionStore((state) => state.login);
  const logout = useAuthSessionStore((state) => state.logout);
  const clearError = useAuthSessionStore((state) => state.clearError);

  const isServerInfoAvailable = serverInfoState === 'ready' && serverInfo !== null;
  const isServerOffline = serverInfoState === 'offline';
  const serverName = serverInfo?.meshName || 'serveur local';
  const serverDomain = isServerInfoAvailable ? serverInfo?.domainName || serverName : null;
  const serverLabel = isServerInfoAvailable ? serverInfo?.domainName || serverInfo?.meshName || 'serveur local' : 'serveur local';
  const serverVersion = isServerInfoAvailable ? serverInfo.adminUiVersion : null;
  const serverUptime = isServerInfoAvailable ? formatUptime(serverInfo.uptimeSeconds) : null;
  const locale = (i18n.resolvedLanguage ?? i18n.language).startsWith('en') ? 'en-GB' : 'fr-FR';
  const headerMeta = [formatHeaderDate(new Date(), locale), serverDomain].filter((value): value is string => Boolean(value)).join(' · ');

  return {
    clearError,
    currentUser,
    errorMessage,
    hasRestoredSession,
    headerMeta,
    isRestoringSession,
    isServerInfoAvailable,
    isServerOffline,
    isSubmitting,
    login,
    logout,
    restoreSession,
    serverInfo,
    serverInfoState,
    serverLabel,
    serverVersion,
    serverUptime,
  };
}