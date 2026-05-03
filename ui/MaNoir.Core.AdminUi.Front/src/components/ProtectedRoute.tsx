import * as React from 'react';
import { Navigate, Outlet } from 'react-router';
import { useTranslation } from 'react-i18next';
import { useAuthSessionStore } from '../stores/authSessionStore';

export function ProtectedRoute() {
  const { t } = useTranslation();
  const currentUser = useAuthSessionStore((state) => state.currentUser);
  const isRestoringSession = useAuthSessionStore((state) => state.isRestoringSession);
  const hasRestoredSession = useAuthSessionStore((state) => state.hasRestoredSession);
  const restoreSession = useAuthSessionStore((state) => state.restoreSession);

  React.useEffect(() => {
    if (!hasRestoredSession) {
      void restoreSession();
    }
  }, [hasRestoredSession, restoreSession]);

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
    return <Navigate replace to="/login" />;
  }

  return <Outlet />;
}