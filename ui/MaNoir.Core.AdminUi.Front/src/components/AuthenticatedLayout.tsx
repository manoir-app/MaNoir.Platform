import * as React from 'react';
import { Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { DefaultAdminShell } from '@manoir-app/core-admin-ui-kit/default-admin-shell';
import manoirLogo from '../../../../../manoir-app.github.io/static/img/logo.svg';
import i18n, { resolveSupportedLanguage } from '../i18n';
import { getLocalMeshSettings } from '../lib/api';
import { useAuthSessionViewModel } from '../hooks/useAuthSessionViewModel';
import { useAuthenticatedLayoutViewModel } from '../hooks/useAuthenticatedLayoutViewModel';
import { LoginPage } from '../pages/LoginPage';

export function AuthenticatedLayout() {
  const { t } = useTranslation();
  const { currentUser, hasRestoredSession, isRestoringSession, restoreSession } = useAuthSessionViewModel();
  const {
    currentUserLabel,
    handleLogout,
    headerMeta,
    isSubmitting,
    navigationItems,
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

  return (
    <DefaultAdminShell
      logoutDisabled={isSubmitting}
      logoutLabel={isSubmitting ? t('common.actions.loggingOut') : t('common.actions.logout')}
      navigationAriaLabel={t('navigation.main')}
      navigationItems={navigationItems}
      onLogout={() => {
        void handleLogout();
      }}
      serverLabel={t('authLayout.currentServer')}
      serverMeta={serverVersion ? <span>{t('common.appName')} {serverVersion}</span> : null}
      serverName={serverLabel}
      serverStatus={serverStatusLabel}
      serverStatusTone={serverStatusTone === 'success' ? 'success' : 'neutral'}
      sidebarBrand={t('common.appName')}
      sidebarDescription={t('authLayout.copy')}
      sidebarEyebrow={t('authLayout.eyebrow')}
      topBarBrand={t('common.appName')}
      topBarLogo={<img alt="Logo MaNoir" src={manoirLogo} />}
      topBarMeta={headerMeta}
      userLabel={t('authLayout.userLabel')}
      userValue={currentUserLabel}
    >
      <Outlet />
    </DefaultAdminShell>
  );
}