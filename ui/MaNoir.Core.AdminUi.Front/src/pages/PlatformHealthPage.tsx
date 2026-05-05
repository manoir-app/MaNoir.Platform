import { useTranslation } from 'react-i18next';
import { useAuthSessionViewModel } from '../hooks/useAuthSessionViewModel';
import { isAgentStale, useRegisteredAgentsData } from '../hooks/useRegisteredAgentsData';

function formatDateTime(date: Date | string | null, locale: string) {
  if (!date) {
    return '—';
  }

  const resolvedDate = typeof date === 'string' ? new Date(date) : date;
  if (Number.isNaN(resolvedDate.getTime())) {
    return '—';
  }

  return new Intl.DateTimeFormat(locale, {
    dateStyle: 'medium',
    timeStyle: 'short',
  }).format(resolvedDate);
}

function getAgentStateTone(state: string, stale: boolean) {
  if (stale || state === 'degraded') {
    return 'danger';
  }

  if (state === 'starting' || state === 'stopping') {
    return 'warning';
  }

  return 'neutral';
}

export function PlatformHealthPage() {
  const { t, i18n } = useTranslation();
  const locale = (i18n.resolvedLanguage ?? i18n.language).startsWith('en') ? 'en-GB' : 'fr-FR';
  const { serverInfo, serverInfoState, serverLabel, serverUptime, serverVersion } = useAuthSessionViewModel();
  const { agents, errorMessage, isLoading, isRefreshing, lastUpdatedAt, refreshAgents, summary } = useRegisteredAgentsData();

  const agentsNeedingAttention = agents.filter((agent) => agent.state === 'degraded' || isAgentStale(agent)).slice(0, 6);

  return (
    <div className="front-observability-page">
      <section className="front-observability-hero">
        <div className="front-login-page-eyebrow">{t('health.eyebrow')}</div>
        <div className="front-observability-hero-row">
          <div>
            <h1 className="front-observability-title">{t('health.title')}</h1>
            <p className="front-observability-copy">{t('health.description')}</p>
          </div>
          <button
            className="front-button front-button-secondary front-button-large"
            disabled={isRefreshing}
            onClick={() => {
              void refreshAgents();
            }}
            type="button"
          >
            {isRefreshing ? t('common.actions.refreshing') : t('common.actions.refresh')}
          </button>
        </div>
        <div className="front-observability-meta-row">
          <span>{t('health.snapshot.server', { server: serverLabel })}</span>
          <span>{t('health.snapshot.updated', { date: formatDateTime(lastUpdatedAt, locale) })}</span>
        </div>
      </section>

      <section className="front-observability-summary-grid" aria-label={t('health.summary.ariaLabel')}>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('health.summary.serverStatus')}</div>
          <div className="front-console-stat-value">{serverInfoState === 'ready' ? t('common.status.online') : t('common.status.offline')}</div>
          <div className="front-console-stat-detail">{serverInfo?.domainName || serverLabel}</div>
        </article>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('health.summary.totalAgents')}</div>
          <div className="front-console-stat-value">{summary.totalCount}</div>
          <div className="front-console-stat-detail">{t('health.summary.readyAgents', { count: summary.readyCount })}</div>
        </article>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('health.summary.degradedAgents')}</div>
          <div className="front-console-stat-value">{summary.degradedCount}</div>
          <div className="front-console-stat-detail">{t('health.summary.needingAttention')}</div>
        </article>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('health.summary.staleAgents')}</div>
          <div className="front-console-stat-value">{summary.staleCount}</div>
          <div className="front-console-stat-detail">{t('health.summary.staleHint')}</div>
        </article>
      </section>

      <section className="front-observability-panel">
        <div className="front-observability-panel-header">
          <div>
            <div className="front-login-page-eyebrow">{t('health.runtime.eyebrow')}</div>
            <h2 className="front-observability-section-title">{t('health.runtime.title')}</h2>
          </div>
        </div>
        <div className="front-observability-detail-grid">
          <article className="front-observability-detail-card">
            <div className="front-console-stat-label">{t('health.runtime.mesh')}</div>
            <div className="front-console-stat-value">{serverInfo?.meshName || serverLabel}</div>
          </article>
          <article className="front-observability-detail-card">
            <div className="front-console-stat-label">{t('health.runtime.domain')}</div>
            <div className="front-console-stat-value">{serverInfo?.domainName || '—'}</div>
          </article>
          <article className="front-observability-detail-card">
            <div className="front-console-stat-label">{t('health.runtime.version')}</div>
            <div className="front-console-stat-value">{serverVersion || t('common.versionUnavailable')}</div>
          </article>
          <article className="front-observability-detail-card">
            <div className="front-console-stat-label">{t('health.runtime.uptime')}</div>
            <div className="front-console-stat-value">{serverUptime || '—'}</div>
          </article>
        </div>
      </section>

      <section className="front-observability-panel">
        <div className="front-observability-panel-header">
          <div>
            <div className="front-login-page-eyebrow">{t('health.watchlist.eyebrow')}</div>
            <h2 className="front-observability-section-title">{t('health.watchlist.title')}</h2>
          </div>
        </div>

        {errorMessage ? <div className="front-observability-feedback front-observability-feedback-error">{errorMessage}</div> : null}
        {isLoading ? <div className="front-observability-feedback">{t('health.watchlist.loading')}</div> : null}
        {!isLoading && !errorMessage && agentsNeedingAttention.length === 0 ? (
          <div className="front-observability-feedback front-observability-feedback-success">{t('health.watchlist.empty')}</div>
        ) : null}

        <div className="front-observability-agent-grid">
          {agentsNeedingAttention.map((agent) => {
            const stale = isAgentStale(agent);
            const stateTone = getAgentStateTone(agent.state, stale);

            return (
              <article className="front-agent-card" key={agent.id}>
                <div className="front-agent-card-header">
                  <div>
                    <h3 className="front-agent-card-title">{agent.displayName || agent.agentId}</h3>
                    <p className="front-agent-card-subtitle">{agent.agentId} · {agent.meshId}</p>
                  </div>
                  <span className={`front-agent-badge front-agent-badge-${stateTone}`}>
                    {stale ? t('health.watchlist.stale') : t(`agentState.${agent.state}`)}
                  </span>
                </div>
                <dl className="front-agent-card-metadata">
                  <div>
                    <dt>{t('agentRegistry.fields.version')}</dt>
                    <dd>{agent.version || '—'}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.lastHeartbeat')}</dt>
                    <dd>{formatDateTime(agent.lastHeartbeatUtc, locale)}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.statusMessage')}</dt>
                    <dd>{agent.statusMessage || '—'}</dd>
                  </div>
                </dl>
              </article>
            );
          })}
        </div>
      </section>
    </div>
  );
}