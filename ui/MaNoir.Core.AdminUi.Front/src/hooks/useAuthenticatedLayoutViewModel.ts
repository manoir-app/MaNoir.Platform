import * as React from 'react';
import { useLocation, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useAdminNavigationStore } from '../stores/adminNavigationStore';
import { useAuthSessionViewModel } from './useAuthSessionViewModel';

export function useAuthenticatedLayoutViewModel() {
  const { t } = useTranslation();
  const location = useLocation();
  const navigate = useNavigate();
  const { currentUser, headerMeta, isServerInfoAvailable, isSubmitting, logout, serverLabel, serverVersion } = useAuthSessionViewModel();
  const domains = useAdminNavigationStore((state) => state.domains);
  const domainsState = useAdminNavigationStore((state) => state.domainsState);
  const domainMenus = useAdminNavigationStore((state) => state.domainMenus);
  const loadDomains = useAdminNavigationStore((state) => state.loadDomains);
  const loadDomain = useAdminNavigationStore((state) => state.loadDomain);

  const activeDomain = React.useMemo(() => {
    const matchingDomain = domains.find((domain) => domain.href && isActivePath(location.pathname, domain.href));
    if (matchingDomain) {
      return matchingDomain.id;
    }

    if (location.pathname.startsWith('/platform') || location.pathname === '/') {
      return 'platform';
    }

    return domains[0]?.id ?? 'platform';
  }, [domains, location.pathname]);

  React.useEffect(() => {
    if (!currentUser) {
      return;
    }

    void loadDomains();
  }, [currentUser, loadDomains]);

  React.useEffect(() => {
    if (!currentUser || !activeDomain || domainsState !== 'ready' || domains.length === 0) {
      return;
    }

    void loadDomain(activeDomain);
  }, [activeDomain, currentUser, domains.length, domainsState, loadDomain]);

  const currentUserLabel = currentUser?.commonName || currentUser?.firstName || currentUser?.id || t('common.currentUserFallback');
  const serverStatusLabel = isServerInfoAvailable ? t('common.status.online') : t('common.status.offline');
  const serverStatusTone = isServerInfoAvailable ? 'success' : 'muted';

  const fallbackDomains = [
    {
      id: 'platform',
      label: t('domains.platform.label'),
      icon: 'platform',
      href: '/platform',
    },
  ];

  const visibleDomains = domains.length > 0
    ? domains
    : domainsState === 'idle' || domainsState === 'loading'
      ? fallbackDomains
      : [];
  const currentDomain = domainMenus[activeDomain];

  const domainTabItems = visibleDomains.map((item) => {
    const href = item.href ?? undefined;
    const isExternal = Boolean(href && /^https?:\/\//i.test(href));

    return {
      id: item.id,
      active: item.id === activeDomain,
      href: isExternal ? href : undefined,
      icon: item.icon,
      label: item.label,
      onSelect: !href || isExternal ? undefined : () => {
        void navigate(href);
      },
    };
  });

  const navigationSections = (currentDomain?.sections ?? []).map((section) => ({
    id: section.id,
    title: section.label,
    items: section.pages.map((page) => {
      const isExternal = /^https?:\/\//i.test(page.href);
      return {
        id: page.id,
        label: page.label,
        active: isActivePath(location.pathname, page.href),
        href: isExternal ? page.href : undefined,
        onClick: !isExternal ? () => {
          void navigate(page.href);
        } : undefined,
      };
    }),
  }));

  const handleLogout = async () => {
    await logout();
  };

  return {
    activeDomain,
    currentUserLabel,
    domainTabItems,
    handleLogout,
    headerMeta,
    isSubmitting,
    navigationSections,
    serverLabel,
    serverStatusLabel,
    serverStatusTone,
    serverVersion,
  };
}

function isActivePath(currentPath: string, href?: string | null) {
  if (!href || /^https?:\/\//i.test(href)) {
    return false;
  }

  return currentPath === href || currentPath.startsWith(`${href}/`);
}