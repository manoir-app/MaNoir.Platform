import * as React from 'react';
import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { DefaultAdminShell } from '@manoir-app/core-admin-ui-kit/default-admin-shell';
import { ShellHeader } from '@manoir-app/core-admin-ui-kit/shell-header';
import { SidebarNav } from '@manoir-app/core-admin-ui-kit/sidebar-nav';
import manoirLogo from '../assets/logo.svg';
import { DomainIcon } from './DomainIcon';
import i18n, { resolveSupportedLanguage } from '../i18n';
import { getLocalMeshSettings } from '../lib/api';
import { useAuthSessionViewModel } from '../hooks/useAuthSessionViewModel';
import { useAuthenticatedLayoutViewModel } from '../hooks/useAuthenticatedLayoutViewModel';
import { useAdminNavigationStore } from '../stores/adminNavigationStore';
import { LoginPage } from '../pages/LoginPage';

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

  const shellDomainItems = domainTabItems.map((item) => ({
    ...item,
    label: (
      <span className="front-domain-tab-label">
        <DomainIcon kind={'icon' in item ? item.icon : null} />
        <span>{item.label}</span>
      </span>
    ),
  }));

  return (
    <DefaultAdminShell
      logoutDisabled={isSubmitting}
      logoutLabel={isSubmitting ? t('common.actions.loggingOut') : t('common.actions.logout')}
      navigationAriaLabel={t('navigation.main')}
      navigationItems={[]}
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
          sections={navigationSections}
        />
      )}
      topBarBrand={t('common.appName')}
      topBarLogo={<img alt="Logo MaNoir" src={manoirLogo} />}
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