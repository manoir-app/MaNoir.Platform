import { useTranslation } from 'react-i18next';
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

  return 'success';
}

export function AgentRegistryPage() {
  const { t, i18n } = useTranslation();
  const locale = (i18n.resolvedLanguage ?? i18n.language).startsWith('en') ? 'en-GB' : 'fr-FR';
  const { agents, errorMessage, isLoading, isRefreshing, lastUpdatedAt, refreshAgents, summary } = useRegisteredAgentsData();

  return (
    <div className="front-observability-page">
      <section className="front-observability-hero">
        <div className="front-login-page-eyebrow">{t('agentRegistry.eyebrow')}</div>
        <div className="front-observability-hero-row">
          <div>
            <h1 className="front-observability-title">{t('agentRegistry.title')}</h1>
            <p className="front-observability-copy">{t('agentRegistry.description')}</p>
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
          <span>{t('agentRegistry.snapshot.updated', { date: formatDateTime(lastUpdatedAt, locale) })}</span>
        </div>
      </section>

      <section className="front-observability-summary-grid" aria-label={t('agentRegistry.summary.ariaLabel')}>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('agentRegistry.summary.total')}</div>
          <div className="front-console-stat-value">{summary.totalCount}</div>
          <div className="front-console-stat-detail">{t('agentRegistry.summary.registered')}</div>
        </article>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('agentRegistry.summary.ready')}</div>
          <div className="front-console-stat-value">{summary.readyCount}</div>
          <div className="front-console-stat-detail">{t('agentRegistry.summary.readyHint')}</div>
        </article>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('agentRegistry.summary.degraded')}</div>
          <div className="front-console-stat-value">{summary.degradedCount}</div>
          <div className="front-console-stat-detail">{t('agentRegistry.summary.degradedHint')}</div>
        </article>
        <article className="front-observability-summary-card">
          <div className="front-console-stat-label">{t('agentRegistry.summary.stale')}</div>
          <div className="front-console-stat-value">{summary.staleCount}</div>
          <div className="front-console-stat-detail">{t('agentRegistry.summary.staleHint')}</div>
        </article>
      </section>

      <section className="front-observability-panel">
        <div className="front-observability-panel-header">
          <div>
            <div className="front-login-page-eyebrow">{t('agentRegistry.list.eyebrow')}</div>
            <h2 className="front-observability-section-title">{t('agentRegistry.list.title')}</h2>
          </div>
        </div>

        {errorMessage ? <div className="front-observability-feedback front-observability-feedback-error">{errorMessage}</div> : null}
        {isLoading ? <div className="front-observability-feedback">{t('agentRegistry.list.loading')}</div> : null}
        {!isLoading && !errorMessage && agents.length === 0 ? (
          <div className="front-observability-feedback">{t('agentRegistry.list.empty')}</div>
        ) : null}

        <div className="front-observability-agent-grid">
          {agents.map((agent) => {
            const stale = isAgentStale(agent);
            const stateTone = getAgentStateTone(agent.state, stale);

            return (
              <article className="front-agent-card" key={agent.id}>
                <div className="front-agent-card-header">
                  <div>
                    <h3 className="front-agent-card-title">{agent.displayName || agent.agentId}</h3>
                    <p className="front-agent-card-subtitle">{agent.agentId}</p>
                  </div>
                  <span className={`front-agent-badge front-agent-badge-${stateTone}`}>
                    {stale ? t('agentRegistry.states.stale') : t(`agentState.${agent.state}`)}
                  </span>
                </div>

                <dl className="front-agent-card-metadata">
                  <div>
                    <dt>{t('agentRegistry.fields.mesh')}</dt>
                    <dd>{agent.meshId}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.version')}</dt>
                    <dd>{agent.version || '—'}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.registeredAt')}</dt>
                    <dd>{formatDateTime(agent.registeredAtUtc, locale)}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.lastHeartbeat')}</dt>
                    <dd>{formatDateTime(agent.lastHeartbeatUtc, locale)}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.updatedAt')}</dt>
                    <dd>{formatDateTime(agent.updatedAtUtc, locale)}</dd>
                  </div>
                  <div>
                    <dt>{t('agentRegistry.fields.statusMessage')}</dt>
                    <dd>{agent.statusMessage || '—'}</dd>
                  </div>
                </dl>

                <div className="front-agent-card-capabilities">
                  <div className="front-console-stat-label">{t('agentRegistry.fields.capabilities')}</div>
                  <div className="front-agent-chip-list">
                    {agent.capabilities.length === 0 ? (
                      <span className="front-agent-chip front-agent-chip-empty">{t('agentRegistry.capabilities.empty')}</span>
                    ) : (
                      agent.capabilities.map((capability) => (
                        <span className="front-agent-chip" key={capability}>{capability}</span>
                      ))
                    )}
                  </div>
                </div>
              </article>
            );
          })}
        </div>
      </section>
    </div>
  );
}