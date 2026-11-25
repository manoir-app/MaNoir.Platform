import { useTranslation } from 'react-i18next';
import { Button } from '@manoir-app/core-admin-ui-kit/button';
import { Card } from '@manoir-app/core-admin-ui-kit/card';
import { PageHeader } from '@manoir-app/core-admin-ui-kit/page-header';
import { StatusPill } from '@manoir-app/core-admin-ui-kit/status-pill';
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

function getAgentTone(state: string, stale: boolean) {
  if (stale || state === 'degraded') {
    return 'danger' as const;
  }

  if (state === 'starting' || state === 'stopping') {
    return 'warning' as const;
  }

  if (state === 'ready') {
    return 'success' as const;
  }

  return 'neutral' as const;
}

export function PlatformHealthPage() {
  const { t, i18n } = useTranslation();
  const locale = (i18n.resolvedLanguage ?? i18n.language).startsWith('en') ? 'en-GB' : 'fr-FR';
  const { serverInfoState, serverLabel, serverUptime, serverVersion } = useAuthSessionViewModel();
  const { agents, errorMessage, isLoading, isRefreshing, lastUpdatedAt, refreshAgents, summary } = useRegisteredAgentsData();
  const isOnline = serverInfoState === 'ready';

  const platformRows = [
    {
      label: t('monitoring.platformServer'),
      value: (
        <StatusPill tone={isOnline ? 'success' : 'danger'}>
          {isOnline ? t('common.status.online') : t('common.status.offline')}
        </StatusPill>
      ),
    },
    { label: t('health.runtime.mesh'), value: serverLabel },
    { label: t('health.runtime.version'), value: serverVersion || t('common.versionUnavailable') },
    { label: t('health.runtime.uptime'), value: serverUptime || '—' },
    { label: t('monitoring.agentsReady'), value: `${summary.readyCount} / ${summary.totalCount}` },
  ];

  return (
    <div className="front-monitor">
      <PageHeader
        actions={(
          <Button
            disabled={isRefreshing}
            loading={isRefreshing}
            onClick={() => {
              void refreshAgents();
            }}
            variant="secondary"
          >
            {isRefreshing ? t('common.actions.refreshing') : t('common.actions.refresh')}
          </Button>
        )}
        description={t('monitoring.description')}
        meta={lastUpdatedAt ? t('monitoring.updated', { date: formatDateTime(lastUpdatedAt, locale) }) : undefined}
        title={t('monitoring.title')}
        variant="page"
      />

      <div className="front-monitor-grid">
        <Card className="front-monitor-card">
          <h2 className="front-monitor-card-title">{t('monitoring.platform')}</h2>
          <dl className="front-home-facts">
            {platformRows.map((row) => (
              <div className="front-home-fact" key={row.label}>
                <dt>{row.label}</dt>
                <dd>{row.value}</dd>
              </div>
            ))}
          </dl>
        </Card>

        <Card className="front-monitor-card">
          <h2 className="front-monitor-card-title">{t('monitoring.agentsTitle')}</h2>

          {errorMessage ? <p className="front-monitor-feedback front-monitor-feedback-error">{errorMessage}</p> : null}
          {isLoading ? <p className="front-monitor-feedback">{t('agentRegistry.list.loading')}</p> : null}
          {!isLoading && !errorMessage && agents.length === 0 ? (
            <p className="front-monitor-feedback">{t('agentRegistry.list.empty')}</p>
          ) : null}

          <ul className="front-monitor-agents">
            {agents.map((agent) => {
              const stale = isAgentStale(agent);

              return (
                <li className="front-monitor-agent" key={agent.id}>
                  <div className="front-monitor-agent-main">
                    <strong>{agent.displayName || agent.agentId}</strong>
                    <span>
                      {[agent.agentId, agent.version, t('monitoring.lastSignal', { date: formatDateTime(agent.lastHeartbeatUtc, locale) })]
                        .filter(Boolean)
                        .join(' · ')}
                    </span>
                    {agent.statusMessage ? <span>{agent.statusMessage}</span> : null}
                    {agent.capabilities.length > 0 ? (
                      <span className="front-monitor-chips">
                        {agent.capabilities.map((capability) => (
                          <span className="front-monitor-chip" key={capability}>{capability}</span>
                        ))}
                      </span>
                    ) : null}
                  </div>
                  <StatusPill tone={getAgentTone(agent.state, stale)}>
                    {stale ? t('agentRegistry.states.stale') : t(`agentState.${agent.state}`)}
                  </StatusPill>
                </li>
              );
            })}
          </ul>
        </Card>
      </div>
    </div>
  );
}
