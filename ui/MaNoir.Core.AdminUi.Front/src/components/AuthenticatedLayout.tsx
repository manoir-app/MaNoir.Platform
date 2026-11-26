import * as React from 'react';
import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { DefaultAdminShell } from '@manoir-app/core-admin-ui-kit/default-admin-shell';
import { ShellHeader } from '@manoir-app/core-admin-ui-kit/shell-header';
import { SidebarNav } from '@manoir-app/core-admin-ui-kit/sidebar-nav';
import { InlineTabs } from '@manoir-app/core-admin-ui-kit/inline-tabs';
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

function GearIcon() {
  return (
    <svg {...iconProps} viewBox="0 0 24 24">
      <path d="M12.22 2h-.44a2 2 0 0 0-2 2v.18a2 2 0 0 1-1 1.73l-.43.25a2 2 0 0 1-2 0l-.15-.08a2 2 0 0 0-2.73.73l-.22.38a2 2 0 0 0 .73 2.73l.15.1a2 2 0 0 1 1 1.72v.51a2 2 0 0 1-1 1.74l-.15.09a2 2 0 0 0-.73 2.73l.22.38a2 2 0 0 0 2.73.73l.15-.08a2 2 0 0 1 2 0l.43.25a2 2 0 0 1 1 1.73V20a2 2 0 0 0 2 2h.44a2 2 0 0 0 2-2v-.18a2 2 0 0 1 1-1.73l.43-.25a2 2 0 0 1 2 0l.15.08a2 2 0 0 0 2.73-.73l.22-.39a2 2 0 0 0-.73-2.73l-.15-.08a2 2 0 0 1-1-1.74v-.5a2 2 0 0 1 1-1.74l.15-.09a2 2 0 0 0 .73-2.73l-.22-.38a2 2 0 0 0-2.73-.73l-.15.08a2 2 0 0 1-2 0l-.43-.25a2 2 0 0 1-1-1.73V4a2 2 0 0 0-2-2z" />
      <circle cx="12" cy="12" r="3" />
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
    // Icon rail by default; the user's explicit choice wins.
    return window.localStorage.getItem(sidebarCollapsedKey) !== '0';
  } catch {
    return true;
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

  // The settings domain is reached through the gear action, not through the domain switcher.
  const settingsItem = domainTabItems.find((item) => item.id === 'settings');
  const shellDomainItems = domainTabItems
    .filter((item) => item.id !== 'settings')
    .map((item) => ({
      ...item,
      icon: <DomainIcon kind={'icon' in item ? item.icon : null} />,
    }));

  // One sidebar entry per section; the pages of a section become tabs above the content.
  const sectionIconKinds: Record<string, string> = {
    overview: 'grid',
    house: 'map-pin',
    people: 'users',
    monitoring: 'pulse',
    extensions: 'plug',
    system: 'server',
  };
  const endSectionIds = new Set(['system']);
  const sectionEntries = navigationSections
    .filter((section) => section.items.length > 0)
    .map((section) => {
      const [firstPage] = section.items;
      const iconKind = sectionIconKinds[section.id] ?? section.items.find((item) => hasPageIcon(item.iconKind))?.iconKind;
      const sectionTitle = typeof section.title === 'string' ? section.title : '';

      return {
        id: section.id,
        label: i18n.exists(`navigation.sections.${section.id}`) ? t(`navigation.sections.${section.id}` as never) : sectionTitle,
        icon: hasPageIcon(iconKind) ? <PageIcon kind={iconKind} /> : undefined,
        active: section.items.some((item) => item.active),
        href: firstPage.href,
        onClick: firstPage.onClick,
      };
    });
  const sidebarSections = [
    { id: 'sections', items: sectionEntries.filter((entry) => !endSectionIds.has(entry.id)) },
    { id: 'sections-end', align: 'end' as const, items: sectionEntries.filter((entry) => endSectionIds.has(entry.id)) },
  ].filter((section) => section.items.length > 0);
  const activeSection = navigationSections.find((section) => section.items.some((item) => item.active));
  const activeSectionTabs = activeSection && activeSection.items.length > 1 ? activeSection.items : [];

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
          {settingsItem ? (
            <button
              aria-current={settingsItem.active ? 'page' : undefined}
              aria-label={t('navigation.platformSettings')}
              className="front-top-action"
              onClick={settingsItem.onSelect}
              title={t('navigation.platformSettings')}
              type="button"
            >
              <GearIcon />
            </button>
          ) : null}
        </>
      )}
      sidebarFooterActions={(
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
      )}
      topBarLogo="M"
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
      {activeSectionTabs.length > 0 ? (
        <div className="front-section-tabs">
          <InlineTabs
            aria-label={activeSection ? String(activeSection.title ?? '') : undefined}
            items={activeSectionTabs.map((item) => ({
              id: item.id,
              label: item.label,
              onSelect: item.onClick,
              href: item.href,
            }))}
            onValueChange={() => {}}
            value={activeSectionTabs.find((item) => item.active)?.id ?? ''}
            variant="pill"
          />
        </div>
      ) : null}
      <Outlet />
    </DefaultAdminShell>
  );
}