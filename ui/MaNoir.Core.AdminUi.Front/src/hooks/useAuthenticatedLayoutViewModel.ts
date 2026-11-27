import * as React from 'react';
import { useLocation, useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import i18n from '../i18n';
import type { AdminDomainNavigationResponse, AdminNavigationDomainSummary } from '../lib/api';
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
  const selectedDomainId = useAdminNavigationStore((state) => state.selectedDomainId);
  const selectDomain = useAdminNavigationStore((state) => state.selectDomain);

  const activeDomain = React.useMemo(() => {
    // A contribution may target several domains, so one path can belong to more than one of them.
    const matchingDomains = domains.filter((domain) => containsPath(domain, domainMenus[domain.id], location.pathname));
    const matchingDomain = matchingDomains.find((domain) => domain.id === selectedDomainId) ?? matchingDomains[0];
    if (matchingDomain) {
      return matchingDomain.id;
    }

    if (location.pathname.startsWith('/platform') || location.pathname === '/') {
      return 'settings';
    }

    return domains[0]?.id ?? 'settings';
  }, [domainMenus, domains, location.pathname, selectedDomainId]);

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
      id: 'settings',
      label: t('domains.settings.label'),
      icon: 'settings',
      href: '/platform',
      isHostedByModule: false,
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
    const isExternal = leavesThisApp(href, item.isHostedByModule);

    return {
      id: item.id,
      active: item.id === activeDomain,
      href: isExternal ? href : undefined,
      icon: item.icon,
      // Declared domains are translated here; any other keeps the label sent by the API.
      label: i18n.exists(`domains.${item.id}.label`) ? t(`domains.${item.id}.label` as never) : item.label,
      onSelect: !href || isExternal ? undefined : () => {
        selectDomain(item.id);
        void navigate(href);
      },
    };
  });

  const navigationSections = (currentDomain?.sections ?? []).map((section) => ({
    id: section.id,
    title: section.label,
    items: section.pages.map((page) => {
      const isExternal = leavesThisApp(page.href, page.isHostedByModule);
      return {
        id: page.id,
        iconKind: page.icon ?? page.name,
        label: page.label,
        active: !isExternal && isActivePath(location.pathname, page.href),
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

// Links that this router cannot serve are followed by the browser: other sites and the modules' own Admin UIs.
function leavesThisApp(href: string | null | undefined, isHostedByModule: boolean) {
  return Boolean(href && (isHostedByModule || /^https?:\/\//i.test(href)));
}

function containsPath(domain: AdminNavigationDomainSummary, menu: AdminDomainNavigationResponse | undefined, path: string) {
  return (!domain.isHostedByModule && isActivePath(path, domain.href))
    || Boolean(menu?.sections.some((section) => section.pages.some((page) => !page.isHostedByModule && isActivePath(path, page.href))));
}

function isActivePath(currentPath: string, href?: string | null) {
  if (!href || /^https?:\/\//i.test(href)) {
    return false;
  }

  return currentPath === href || currentPath.startsWith(`${href}/`);
}