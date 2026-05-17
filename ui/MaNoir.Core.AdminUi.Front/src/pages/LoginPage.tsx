import * as React from 'react';
import { Trans, useTranslation } from 'react-i18next';
import { AppBrandHeader } from '@manoir-app/core-admin-ui-kit/app-brand-header';
import { LoginPage as KitLoginPage } from '@manoir-app/core-admin-ui-kit/login-page';
import manoirLogo from '../assets/logo.svg';
import { LanguageSwitcher } from '../components/LanguageSwitcher';
import { useAuthSessionViewModel } from '../hooks/useAuthSessionViewModel';

export function LoginPage() {
  const { t } = useTranslation();
  const [userId, setUserId] = React.useState('');
  const [password, setPassword] = React.useState('');
  const [showPassword, setShowPassword] = React.useState(false);
  const [remember, setRemember] = React.useState(true);
  const {
    clearError,
    errorMessage,
    headerMeta,
    isServerInfoAvailable,
    isServerOffline,
    isSubmitting,
    login,
    serverLabel,
    serverVersion,
    serverUptime,
  } = useAuthSessionViewModel();

  const handleLoginSubmit = async (event: React.FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    clearError();

    try {
      await login(
        {
          userId: userId.trim(),
          password,
        },
        {
          isInteractive: true,
        },
      );
    } catch {
    }
  };

  const isLoginBlocked = isSubmitting || isServerOffline;

  const serverSummary = (
    <div className={`front-login-server-card${isServerInfoAvailable ? '' : ' front-login-server-card-offline'}`}>
      <div className="front-login-server-eyebrow">{t('login.server.detected')}</div>
      <div className="front-login-server-row">
        <div className={`front-login-server-glyph${isServerInfoAvailable ? '' : ' front-login-server-glyph-offline'}`} aria-hidden="true">
          ⌂
        </div>
        <div className="front-login-server-copy">
          <div className={`front-login-server-name${isServerInfoAvailable ? '' : ' front-login-server-name-offline'}`}>
            {isServerInfoAvailable ? serverLabel : t('common.status.offline')}
          </div>
          {isServerInfoAvailable ? (
            <div className="front-login-server-meta">{t('login.server.meta', { version: serverVersion ?? '', uptime: serverUptime ?? '' })}</div>
          ) : (
            <div className="front-login-server-meta front-login-server-meta-offline">{t('login.server.unavailable')}</div>
          )}
        </div>
        <div className="front-login-status-row">
          <span aria-hidden="true" className={`front-login-status-dot ${isServerInfoAvailable ? 'front-login-status-dot-success' : 'front-login-status-dot-offline'}`} />
          <span className={`front-login-status-label${isServerInfoAvailable ? '' : ' front-login-status-label-offline'}`}>
            {isServerInfoAvailable ? t('common.status.online') : t('common.status.offline')}
          </span>
        </div>
      </div>
    </div>
  );

  const heroFooter = (
    <div className="front-login-hero-footer">
      <span>{t('common.timezone')}</span>
      {serverVersion ? (
        <>
          <span>·</span>
          <span>v {serverVersion}</span>
        </>
      ) : (
        <span className="front-login-hero-footer-muted">{t('common.versionUnavailable')}</span>
      )}
    </div>
  );

  const credentialsBody = (
    <form
      className={`front-login-form${isServerOffline ? ' front-login-form-disabled' : ''}`}
      onSubmit={handleLoginSubmit}
    >
      <div className="front-login-form-fields">
        <label className="front-login-field">
          <span className="front-login-field-header">
            <span className="front-login-field-label">{t('login.form.identifier')}</span>
            <span className="front-login-field-aside">{t('login.form.identifierAside')}</span>
          </span>
          <input
            autoComplete="username"
            className="front-login-input"
            disabled={isLoginBlocked}
            onChange={(event) => setUserId(event.target.value)}
            type="text"
            value={userId}
          />
        </label>

        <label className="front-login-field">
          <span className="front-login-field-header">
            <span className="front-login-field-label">{t('login.form.password')}</span>
          </span>
          <div className="front-login-password-row">
            <input
              autoComplete="current-password"
              className="front-login-password-input"
              disabled={isLoginBlocked}
              onChange={(event) => setPassword(event.target.value)}
              type={showPassword ? 'text' : 'password'}
              value={password}
            />
            <button className="front-login-inline-button" disabled={isLoginBlocked} onClick={() => setShowPassword((current) => !current)} type="button">
              {showPassword ? t('common.actions.hide') : t('common.actions.show')}
            </button>
          </div>
        </label>

        <div className="front-login-form-meta-row">
          <label className="front-login-checkbox-row">
            <input
              checked={remember}
              className="front-login-checkbox"
              disabled={isLoginBlocked}
              onChange={(event) => setRemember(event.target.checked)}
              type="checkbox"
            />
            <span>{t('login.form.remember')}</span>
          </label>
          <span className={`front-login-link${isLoginBlocked ? ' front-login-link-disabled' : ''}`}>{t('login.form.forgotPassword')}</span>
        </div>

        {isServerOffline ? <div className="front-login-feedback">{t('login.form.offlineFeedback')}</div> : null}
        {errorMessage ? <div className="front-login-feedback front-login-feedback-error">{errorMessage}</div> : null}
      </div>

      <div className="front-login-form-actions">
        <button className="front-button front-button-primary front-button-large front-login-primary-button" disabled={isLoginBlocked} type="submit">
          {isServerOffline ? t('login.form.offlineButton') : isSubmitting ? t('login.form.submitBusy') : t('login.form.submitIdle')}
        </button>
      </div>
    </form>
  );

  return (
    <KitLoginPage
      heroDescription={t('login.hero.description')}
      heroEyebrow={t('login.eyebrow')}
      heroFooter={heroFooter}
      heroSupplementary={serverSummary}
      heroTitle={<Trans components={{ em: <em /> }} i18nKey="login.hero.title" />}
      panelDescription={
        isServerOffline
          ? t('login.session.offlineDescription')
          : <Trans components={[<></>, <span className="front-login-panel-mono" />]} i18nKey="login.session.loginDescription" values={{ server: serverLabel }} />
      }
      panelEyebrow={isServerOffline ? t('login.session.offline') : t('login.session.user')}
      panelFooter={
        <div className="front-login-link-row-centered">
          <span>{t('login.authenticated.firstTime')}</span>
          <a className="front-login-link-strong" href="#">
            {t('login.authenticated.requestAccess')}
          </a>
        </div>
      }
      panelTitle={isServerOffline ? t('login.session.unavailable') : t('login.session.welcomeBack')}
      topBar={(
        <AppBrandHeader
          actions={(
            <>
              <LanguageSwitcher />
              <div className="front-login-page-topbar-status">{isServerInfoAvailable ? t('login.topbarStatusSecure') : t('common.status.offline')}</div>
            </>
          )}
          brand={t('common.appName')}
          logo={<img alt="Logo MaNoir" src={manoirLogo} />}
          meta={headerMeta}
        />
      )}
    >
      {credentialsBody}
    </KitLoginPage>
  );
}