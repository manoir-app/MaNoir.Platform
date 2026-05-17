import { useTranslation } from 'react-i18next';
import { useAuthSessionViewModel } from './useAuthSessionViewModel';

export function useConsoleHomePageViewModel() {
  const { t } = useTranslation();
  const { currentUser, isServerInfoAvailable, serverLabel, serverUptime, serverVersion } = useAuthSessionViewModel();

  const primaryStats = [
    {
      label: t('console.stats.instance'),
      value: serverLabel,
      detail: isServerInfoAvailable ? t('console.stats.versionDetail', { version: serverVersion ?? t('common.versionUnavailable') }) : t('console.stats.serverOffline'),
    },
    {
      label: t('console.stats.uptime'),
      value: serverUptime ?? t('common.versionUnavailable'),
      detail: t('console.stats.uptimeDetail'),
    },
    {
      label: t('console.stats.operator'),
      value: currentUser?.commonName || currentUser?.firstName || currentUser?.id || t('common.currentUserFallback'),
      detail: currentUser?.mainEmail || t('common.publicEmailUnavailable'),
    },
  ];

  const workstreams = [
    {
      title: t('console.workstreams.administration.title'),
      description: t('console.workstreams.administration.description'),
    },
    {
      title: t('console.workstreams.automation.title'),
      description: t('console.workstreams.automation.description'),
    },
    {
      title: t('console.workstreams.extensions.title'),
      description: t('console.workstreams.extensions.description'),
    },
  ];

  return {
    primaryStats,
    workstreams,
  };
}