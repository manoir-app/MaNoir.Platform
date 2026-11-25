import * as React from 'react';
import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { DefaultAdminShell } from '@manoir-app/core-admin-ui-kit/default-admin-shell';
import { ShellHeader } from '@manoir-app/core-admin-ui-kit/shell-header';
import { SidebarNav } from '@manoir-app/core-admin-ui-kit/sidebar-nav';
import manoirLogo from '../assets/logo.svg';
import { DomainIcon } from './DomainIcon';
import { hasPageIcon, PageIcon } from './PageIcon';
import i18n, { resolveSupportedLanguage } from '../i18n';
import { getLocalMeshSettings } from '../lib/api';
import { useAuthSessionViewModel } from '../hooks/useAuthSessionViewModel';
import { useAuthenticatedLayoutViewModel } from '../hooks/useAuthenticatedLayoutViewModel';
import { useAdminNavigationStore } from '../stores/adminNavigationStore';
import { LoginPage } from '../pages/LoginPage';

const themeKey = 'manoir.admin.theme';

function readTheme(): 'light' | 'dark' {
  try {
    const stored = window.localStorage.getItem(themeKey);
    if (stored === 'light' || stored === 'dark') {
      return stored;
    }
  } catch {
    // Ignore storage failures.
  }

  return window.matchMedia?.('(prefers-color-scheme: dark)').matches ? 'dark' : 'light';
}

const iconProps = {
  'aria-hidden': true,
  fill: 'none',
  stroke: 'currentColor',
  strokeLinecap: 'round' as const,
  strokeLinejoin: 'round' as const,
  strokeWidth: 1.6,
  viewBox: '0 0 20 20',
};

function PlugIcon() {
  return (
    <svg {...iconProps}>
      <path d="M7 2.5v4M13 2.5v4M5 6.5h10v3a5 5 0 0 1-10 0v-3ZM10 14.5v3" />
    </svg>
  );
}

function SunIcon() {
  return (
    <svg {...iconProps}>
      <circle cx="10" cy="10" r="3.2" />
      <path d="M10 2.5v1.8M10 15.7v1.8M2.5 10h1.8M15.7 10h1.8M4.7 4.7l1.3 1.3M14 14l1.3 1.3M4.7 15.3 6 14M14 6l1.3-1.3" />
    </svg>
  );
}

function MoonIcon() {
  return (
    <svg {...iconProps}>
      <path d="M16.5 11.5A6.5 6.5 0 0 1 8.5 3.5a6.5 6.5 0 1 0 8 8Z" />
    </svg>
  );
}

const sidebarCollapsedKey = 'manoir.admin.sidebarCollapsed';

function readSidebarCollapsed() {
  try {
    return window.localStorage.getItem(sidebarCollapsedKey) === '1';
  } catch {
    return false;
  }
}

export function AuthenticatedLayout() {
  const { t } = useTranslation();
  const { currentUser, hasRestoredSession, isRestoringSession, restoreSession } = useAuthSessionViewModel();
  const clearNavigation = useAdminNavigationStore((state) => state.clear);
  const {
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
  } = useAuthenticatedLayoutViewModel();
  const [sidebarCollapsed, setSidebarCollapsed] = React.useState(readSidebarCollapsed);
  const [theme, setTheme] = React.useState(readTheme);

  React.useEffect(() => {
    document.documentElement.dataset.theme = theme;
    document.documentElement.style.colorScheme = theme;

    try {
      window.localStorage.setItem(themeKey, theme);
    } catch {
      // Ignore storage failures.
    }
  }, [theme]);

  React.useEffect(() => {
    try {
      window.localStorage.setItem(sidebarCollapsedKey, sidebarCollapsed ? '1' : '0');
    } catch {
      // The preference is a convenience; ignore storage failures.
    }
  }, [sidebarCollapsed]);

  React.useEffect(() => {
    if (!hasRestoredSession) {
      void restoreSession();
    }
  }, [hasRestoredSession, restoreSession]);

  React.useEffect(() => {
    if (!currentUser) {
      return undefined;
    }

    let isCancelled = false;

    void getLocalMeshSettings()
      .then((settings) => {
        if (isCancelled) {
          return;
        }

        const meshLanguage = resolveSupportedLanguage(settings.languageId);
        if (meshLanguage && i18n.resolvedLanguage !== meshLanguage) {
          void i18n.changeLanguage(meshLanguage);
        }
      })
      .catch(() => {
      });

    return () => {
      isCancelled = true;
    };
  }, [currentUser]);

  React.useEffect(() => {
    if (currentUser) {
      return undefined;
    }

    clearNavigation();
    return undefined;
  }, [clearNavigation, currentUser]);

  if (isRestoringSession || !hasRestoredSession) {
    return (
      <div className="front-placeholder-page">
        <div className="front-placeholder-card">
          <div className="front-login-page-eyebrow">{t('protectedRoute.eyebrow')}</div>
          <h1 className="front-placeholder-title">{t('protectedRoute.title')}</h1>
          <p className="front-placeholder-copy">
            {t('protectedRoute.description')}
          </p>
        </div>
      </div>
    );
  }

  if (!currentUser) {
    return <LoginPage />;
  }

  const platformItem = domainTabItems.find((item) => item.id === 'platform');
  const shellDomainItems = domainTabItems
    .filter((item) => item.id !== 'platform')
    .map((item) => ({
      ...item,
      icon: <DomainIcon kind={'icon' in item ? item.icon : null} />,
    }));

  const sidebarSections = navigationSections.map((section) => ({
    ...section,
    items: section.items.map(({ iconKind, ...item }) => ({
      ...item,
      icon: hasPageIcon(iconKind) ? <PageIcon kind={iconKind} /> : undefined,
    })),
  }));

  return (
    <DefaultAdminShell
      contentPadding="compact"
      logoutDisabled={isSubmitting}
      logoutLabel={isSubmitting ? t('common.actions.loggingOut') : t('common.actions.logout')}
      navigationAriaLabel={t('navigation.main')}
      navigationItems={[]}
      onSidebarCollapsedChange={setSidebarCollapsed}
      sidebarCollapsed={sidebarCollapsed}
      sidebarToggleLabel={t('navigation.toggleSidebar')}
      onLogout={() => {
        void handleLogout();
      }}
      serverLabel={t('authLayout.currentServer')}
      serverMeta={serverVersion ? <span>{t('common.appName')} {serverVersion}</span> : null}
      serverName={serverLabel}
      serverStatus={serverStatusLabel}
      serverStatusTone={serverStatusTone === 'success' ? 'success' : 'neutral'}
      sidebarBrand={t('common.appName')}
      sidebarEyebrow={t('authLayout.eyebrow')}
      sidebarNavigation={(
        <SidebarNav
          aria-label={t('navigation.main')}
          className="front-auth-sidebar-nav"
          collapsed={sidebarCollapsed}
          sections={sidebarSections}
        />
      )}
      topBarBrand={t('common.appName')}
      topBarActions={(
        <>
          {platformItem ? (
            <button
              aria-current={platformItem.active ? 'page' : undefined}
              aria-label={t('navigation.platformSettings')}
              className="front-top-action"
              onClick={platformItem.onSelect}
              title={t('navigation.platformSettings')}
              type="button"
            >
              <PlugIcon />
            </button>
          ) : null}
          <button
            aria-label={t('navigation.switchTheme')}
            className="front-top-action"
            onClick={() => {
              setTheme((current) => (current === 'dark' ? 'light' : 'dark'));
            }}
            title={t('navigation.switchTheme')}
            type="button"
          >
            {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
          </button>
        </>
      )}
      topBarLogo={<img alt="" src={manoirLogo} />}
      topBarMeta={headerMeta}
      topBarNavigation={(
        <ShellHeader
          aria-label={t('domains.ariaLabel')}
          className="front-domain-shell-header"
          compactBreakpoint={760}
          items={shellDomainItems}
          overflowLabel={t('domains.more')}
        />
      )}
      userLabel={t('authLayout.userLabel')}
      userValue={currentUserLabel}
    >
      <Outlet />
    </DefaultAdminShell>
  );
}