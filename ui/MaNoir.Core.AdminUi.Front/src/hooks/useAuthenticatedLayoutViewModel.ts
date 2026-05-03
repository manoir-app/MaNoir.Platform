import { useLocation, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useAuthSessionViewModel } from './useAuthSessionViewModel';

export function useAuthenticatedLayoutViewModel() {
  const { t } = useTranslation();
  const location = useLocation();
  const navigate = useNavigate();
  const { currentUser, headerMeta, isServerInfoAvailable, isSubmitting, logout, serverLabel, serverVersion } = useAuthSessionViewModel();

  const navigationCatalog = [
    {
      label: t('navigation.console.label'),
      description: t('navigation.console.description'),
      to: '/console',
    },
  ] as const;

  const currentUserLabel = currentUser?.commonName || currentUser?.firstName || currentUser?.id || t('common.currentUserFallback');
  const serverStatusLabel = isServerInfoAvailable ? t('common.status.online') : t('common.status.offline');
  const serverStatusTone = isServerInfoAvailable ? 'success' : 'muted';

  const navigationItems = navigationCatalog.map((item) => ({
    ...item,
    id: item.to,
    isActive: location.pathname.startsWith(item.to),
    onSelect: () => {
      void navigate(item.to);
    },
  }));

  const handleLogout = async () => {
    await logout();
  };

  return {
    currentUserLabel,
    handleLogout,
    headerMeta,
    isSubmitting,
    navigationItems,
    serverLabel,
    serverStatusLabel,
    serverStatusTone,
    serverVersion,
  };
}