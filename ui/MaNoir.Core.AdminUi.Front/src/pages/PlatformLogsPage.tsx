import * as React from 'react';
import { useTranslation } from 'react-i18next';
import { getLogEntries, getLogServices, type PlatformLogEntryModel, type PlatformLogQueryResponseModel } from '../lib/api';

const lookbackOptions = [1, 6, 24, 72];
const limitOptions = [50, 100, 200, 500];
const hostLabelKeys = ['host', 'host_name', 'host.name', 'hostname', 'machine_name', 'server_name', 'service_instance_id'];

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

function formatCompactTimestamp(date: Date | string | null, locale: string) {
  if (!date) {
    return '—';
  }

  const resolvedDate = typeof date === 'string' ? new Date(date) : date;
  if (Number.isNaN(resolvedDate.getTime())) {
    return '—';
  }

  return new Intl.DateTimeFormat(locale, {
    month: 'short',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
  }).format(resolvedDate);
}

function getServiceName(entry: PlatformLogEntryModel, fallbackServiceName?: string | null) {
  return entry.labels?.service_name || entry.labels?.['service.name'] || fallbackServiceName || '—';
}

function getHostName(entry: PlatformLogEntryModel) {
  for (const key of hostLabelKeys) {
    const value = entry.labels?.[key];
    if (value) {
      return value;
    }
  }

  return '—';
}

function getLabelEntries(entry: PlatformLogEntryModel) {
  return Object.entries(entry.labels ?? {}).sort(([leftKey], [rightKey]) => leftKey.localeCompare(rightKey));
}

export function PlatformLogsPage() {
  const { t, i18n } = useTranslation();
  const locale = (i18n.resolvedLanguage ?? i18n.language).startsWith('en') ? 'en-GB' : 'fr-FR';
  const [services, setServices] = React.useState<string[]>([]);
  const [selectedServiceName, setSelectedServiceName] = React.useState('');
  const [contains, setContains] = React.useState('');
  const [lookbackHours, setLookbackHours] = React.useState(6);
  const [limit, setLimit] = React.useState(200);
  const [response, setResponse] = React.useState<PlatformLogQueryResponseModel | null>(null);
  const [errorMessage, setErrorMessage] = React.useState<string | null>(null);
  const [isLoading, setIsLoading] = React.useState(true);
  const [isRefreshing, setIsRefreshing] = React.useState(false);
  const [lastUpdatedAt, setLastUpdatedAt] = React.useState<Date | null>(null);
  const [selectedEntryIndex, setSelectedEntryIndex] = React.useState<number | null>(null);

  const loadLogs = async (options?: {
    manualRefresh?: boolean;
    serviceName?: string;
    contains?: string;
    lookbackHours?: number;
    limit?: number;
  }) => {
    const manualRefresh = options?.manualRefresh ?? false;
    const resolvedServiceName = options?.serviceName ?? selectedServiceName;
    const resolvedContains = options?.contains ?? contains;
    const resolvedLookbackHours = options?.lookbackHours ?? lookbackHours;
    const resolvedLimit = options?.limit ?? limit;

    if (manualRefresh) {
      setIsRefreshing(true);
    } else {
      setIsLoading(true);
    }

    setErrorMessage(null);

    const end = new Date();
    const start = new Date(end.getTime() - resolvedLookbackHours * 60 * 60 * 1000);

    try {
      const [serviceNames, logResponse] = await Promise.all([
        getLogServices(),
        getLogEntries({
          serviceName: resolvedServiceName || undefined,
          contains: resolvedContains || undefined,
          limit: resolvedLimit,
          direction: 'backward',
          startUtc: start.toISOString(),
          endUtc: end.toISOString(),
        }),
      ]);

      setServices(serviceNames ?? []);
      setResponse(logResponse);
      setLastUpdatedAt(new Date());
      setSelectedEntryIndex((logResponse.entries?.length ?? 0) > 0 ? 0 : null);
    } catch (error) {
      setErrorMessage(error instanceof Error ? error.message : 'Unable to load the current logs.');
    } finally {
      if (manualRefresh) {
        setIsRefreshing(false);
      } else {
        setIsLoading(false);
      }
    }
  };

  React.useEffect(() => {
    let isDisposed = false;

    const loadInitialState = async () => {
      try {
        await loadLogs({ manualRefresh: false, limit, lookbackHours });

        if (isDisposed) {
          return;
        }
      } catch (error) {
        if (isDisposed) {
          return;
        }

        setErrorMessage(error instanceof Error ? error.message : 'Unable to load the current logs.');
      } finally {
        if (!isDisposed) {
          setIsLoading(false);
        }
      }
    };

    void loadInitialState();

    return () => {
      isDisposed = true;
    };
  }, []);

  const entries = response?.entries ?? [];
  const serviceFacetNames = Array.from(new Set([...services, ...entries.map((entry) => getServiceName(entry, response?.serviceName)).filter((value) => value && value !== '—')]))
    .sort((left, right) => left.localeCompare(right, undefined, { sensitivity: 'base' }));
  const serviceCounts = serviceFacetNames.reduce<Record<string, number>>((accumulator, serviceName) => {
    accumulator[serviceName] = entries.filter((entry) => getServiceName(entry, response?.serviceName) === serviceName).length;
    return accumulator;
  }, {});
  const selectedEntry = selectedEntryIndex != null ? entries[selectedEntryIndex] ?? null : null;

  React.useEffect(() => {
    if (!entries.length) {
      if (selectedEntryIndex !== null) {
        setSelectedEntryIndex(null);
      }

      return;
    }

    if (selectedEntryIndex == null || selectedEntryIndex >= entries.length) {
      setSelectedEntryIndex(0);
    }
  }, [entries.length, selectedEntryIndex]);

  return (
    <div className="front-observability-page front-log-explorer-page">
      <section className="front-observability-hero front-log-explorer-hero">
        <div className="front-login-page-eyebrow">{t('logsBrowser.eyebrow')}</div>
        <div className="front-observability-hero-row">
          <div>
            <h1 className="front-observability-title">{t('logsBrowser.title')}</h1>
            <p className="front-observability-copy">{t('logsBrowser.description')}</p>
          </div>
          <button
            className="front-button front-button-secondary front-button-large"
            disabled={isRefreshing}
            onClick={() => {
              void loadLogs({ manualRefresh: true });
            }}
            type="button"
          >
            {isRefreshing ? t('common.actions.refreshing') : t('common.actions.refresh')}
          </button>
        </div>
        <div className="front-observability-meta-row front-log-explorer-meta-row">
          <span>{t('logsBrowser.snapshot.updated', { date: formatDateTime(lastUpdatedAt, locale) })}</span>
          <span>{t('logsBrowser.results.count', { count: entries.length })}</span>
          <span>{selectedServiceName || t('logsBrowser.sidebar.allServices')}</span>
        </div>
      </section>

      <form
        className="front-log-toolbar"
        onSubmit={(event) => {
          event.preventDefault();
          void loadLogs({ manualRefresh: true });
        }}
      >
        <label className="front-log-toolbar-search">
          <span className="front-console-stat-label">{t('logsBrowser.toolbar.query')}</span>
          <input
            className="front-dialog-input front-log-toolbar-input"
            onChange={(event) => {
              setContains(event.target.value);
            }}
            placeholder={t('logsBrowser.toolbar.queryPlaceholder')}
            type="text"
            value={contains}
          />
        </label>

        <label className="front-log-toolbar-control">
          <span className="front-console-stat-label">{t('logsBrowser.filters.lookback')}</span>
          <select
            className="front-dialog-input"
            onChange={(event) => {
              setLookbackHours(Number(event.target.value));
            }}
            value={String(lookbackHours)}
          >
            {lookbackOptions.map((option) => (
              <option key={option} value={String(option)}>{option}h</option>
            ))}
          </select>
        </label>

        <label className="front-log-toolbar-control">
          <span className="front-console-stat-label">{t('logsBrowser.filters.limit')}</span>
          <select
            className="front-dialog-input"
            onChange={(event) => {
              setLimit(Number(event.target.value));
            }}
            value={String(limit)}
          >
            {limitOptions.map((option) => (
              <option key={option} value={String(option)}>{option}</option>
            ))}
          </select>
        </label>

        <div className="front-log-toolbar-actions">
          <button className="front-button front-button-primary" disabled={isRefreshing} type="submit">
            {t('logsBrowser.toolbar.run')}
          </button>
        </div>
      </form>

      <section className="front-log-workbench">
        <aside className="front-log-sidebar">
          <div className="front-log-sidebar-section">
            <div className="front-login-page-eyebrow">{t('logsBrowser.sidebar.title')}</div>
            <h2 className="front-observability-section-title front-log-sidebar-heading">{t('logsBrowser.sidebar.services')}</h2>
            <p className="front-log-sidebar-copy">{t('logsBrowser.sidebar.servicesHint', { count: services.length })}</p>
          </div>

          <div className="front-log-facet-list">
            <button
              className={`front-log-facet-button${selectedServiceName ? '' : ' front-log-facet-button-active'}`}
              onClick={() => {
                setSelectedServiceName('');
                void loadLogs({ manualRefresh: true, serviceName: '' });
              }}
              type="button"
            >
              <span>{t('logsBrowser.sidebar.allServices')}</span>
              <span className="front-log-facet-count">{entries.length}</span>
            </button>

            {serviceFacetNames.map((serviceName) => (
              <button
                className={`front-log-facet-button${selectedServiceName === serviceName ? ' front-log-facet-button-active' : ''}`}
                key={serviceName}
                onClick={() => {
                  setSelectedServiceName(serviceName);
                  void loadLogs({ manualRefresh: true, serviceName });
                }}
                type="button"
              >
                <span>{serviceName}</span>
                <span className="front-log-facet-count">{serviceCounts[serviceName] ?? 0}</span>
              </button>
            ))}
          </div>
        </aside>

        <section className="front-log-results-panel">
          <div className="front-log-results-header">
            <div>
              <div className="front-login-page-eyebrow">{t('logsBrowser.results.eyebrow')}</div>
              <h2 className="front-observability-section-title">{t('logsBrowser.results.title')}</h2>
            </div>
            {response ? (
              <div className="front-log-query-banner">
                <span className="front-console-stat-label">{t('logsBrowser.results.query')}</span>
                <code className="front-log-query-code">{response.query}</code>
              </div>
            ) : null}
          </div>

          {errorMessage ? <div className="front-observability-feedback front-observability-feedback-error">{errorMessage}</div> : null}
          {isLoading ? <div className="front-observability-feedback">{t('logsBrowser.results.loading')}</div> : null}
          {!isLoading && !errorMessage && entries.length === 0 ? (
            <div className="front-observability-feedback">{t('logsBrowser.results.empty')}</div>
          ) : null}

          <div className="front-log-grid" aria-label={t('logsBrowser.results.title')}>
            <div className="front-log-grid-header">
              <div className="front-log-grid-row front-log-grid-row-header">
                <span className="front-log-grid-cell">{t('logsBrowser.results.columns.date')}</span>
                <span className="front-log-grid-cell">{t('logsBrowser.results.columns.host')}</span>
                <span className="front-log-grid-cell">{t('logsBrowser.results.columns.service')}</span>
                <span className="front-log-grid-cell">{t('logsBrowser.results.columns.content')}</span>
              </div>
            </div>

            <div className="front-log-grid-body">
              {entries.map((entry, index) => {
                const serviceName = getServiceName(entry, response?.serviceName);
                const hostName = getHostName(entry);

                return (
                  <button
                    className={`front-log-grid-row front-log-grid-row-button${selectedEntryIndex === index ? ' front-log-grid-row-active' : ''}`}
                    key={`${entry.timestampUtc}:${index}`}
                    onClick={() => {
                      setSelectedEntryIndex(index);
                    }}
                    type="button"
                  >
                    <span className="front-log-grid-cell front-log-grid-date">{formatCompactTimestamp(entry.timestampUtc, locale)}</span>
                    <span className="front-log-grid-cell front-log-grid-host">{hostName}</span>
                    <span className="front-log-grid-cell front-log-grid-service">{serviceName}</span>
                    <span className="front-log-grid-cell front-log-grid-content">{entry.message}</span>
                  </button>
                );
              })}
            </div>
          </div>
        </section>

        <aside className="front-log-detail-panel">
          <div className="front-login-page-eyebrow">{t('logsBrowser.detail.eyebrow')}</div>
          <h2 className="front-observability-section-title">{t('logsBrowser.detail.title')}</h2>

          {!selectedEntry ? (
            <div className="front-observability-feedback">{t('logsBrowser.detail.empty')}</div>
          ) : (
            <>
              <div className="front-log-detail-meta">
                <div className="front-log-detail-meta-row">
                  <span className="front-console-stat-label">{t('logsBrowser.detail.timestamp')}</span>
                  <span>{formatDateTime(selectedEntry.timestampUtc, locale)}</span>
                </div>
                <div className="front-log-detail-meta-row">
                  <span className="front-console-stat-label">{t('logsBrowser.detail.service')}</span>
                  <span>{getServiceName(selectedEntry, response?.serviceName)}</span>
                </div>
                <div className="front-log-detail-meta-row">
                  <span className="front-console-stat-label">{t('logsBrowser.detail.host')}</span>
                  <span>{getHostName(selectedEntry)}</span>
                </div>
              </div>

              <div className="front-log-detail-block">
                <div className="front-console-stat-label">{t('logsBrowser.detail.message')}</div>
                <pre className="front-log-entry-message front-log-detail-message">{selectedEntry.message}</pre>
              </div>

              <div className="front-log-detail-block">
                <div className="front-console-stat-label">{t('logsBrowser.detail.labels')}</div>
                <div className="front-log-label-list">
                  {getLabelEntries(selectedEntry).map(([key, value]) => (
                    <div className="front-log-label-item" key={key}>
                      <span className="front-log-label-key">{key}</span>
                      <span className="front-log-label-value">{value}</span>
                    </div>
                  ))}
                </div>
              </div>
            </>
          )}
        </aside>
      </section>
    </div>
  );
}