import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { getInstalledPlugins, type InstalledPluginModel } from '../lib/api';

export function InstalledPluginsPage() {
  const { t } = useTranslation();
  const [plugins, setPlugins] = useState<InstalledPluginModel[]>([]);
  const [activeDomain, setActiveDomain] = useState('all');
  const [isLoading, setIsLoading] = useState(true);
  const [hasError, setHasError] = useState(false);

  useEffect(() => {
    let isCancelled = false;
    void getInstalledPlugins()
      .then((result) => {
        if (!isCancelled) setPlugins(result);
      })
      .catch(() => {
        if (!isCancelled) setHasError(true);
      })
      .finally(() => {
        if (!isCancelled) setIsLoading(false);
      });
    return () => { isCancelled = true; };
  }, []);

  const domains = useMemo(() => ['all', ...new Set(plugins.flatMap((plugin) => plugin.contributions.map((contribution) => contribution.tags[0]).filter(Boolean)))], [plugins]);
  const visiblePlugins = plugins.filter((plugin) => activeDomain === 'all' || plugin.contributions.some((contribution) => contribution.tags.includes(activeDomain)));
  const contributionCount = plugins.reduce((total, plugin) => total + plugin.contributions.length, 0);

  return (
    <div className="front-extensions-page">
      <section className="front-extensions-hero">
        <div className="front-login-page-eyebrow">{t('extensions.installed.eyebrow')}</div>
        <h1 className="front-extensions-title">{t('extensions.installed.title')}</h1>
        <p className="front-extensions-copy">{t('extensions.installed.description')}</p>
      </section>
      <section className="front-extensions-stats" aria-label={t('extensions.installed.summaryLabel')}>
        <div><strong>{plugins.length}</strong><span>{t('extensions.installed.pluginCount')}</span></div>
        <div><strong>{contributionCount}</strong><span>{t('extensions.installed.contributionCount')}</span></div>
        <div><strong>0</strong><span>{t('extensions.installed.updates')}</span></div>
      </section>
      <section className="front-extensions-toolbar">
        <div className="front-extensions-tabs" role="tablist" aria-label={t('extensions.installed.domainsLabel')}>
          {domains.map((domain) => (
            <button className={`front-extensions-tab ${activeDomain === domain ? 'front-extensions-tab-active' : ''}`} key={domain} onClick={() => setActiveDomain(domain)} role="tab" aria-selected={activeDomain === domain} type="button">
              {domain === 'all' ? t('extensions.domains.all') : domain}
            </button>
          ))}
        </div>
      </section>
      <section className="front-plugin-list" aria-live="polite">
        {isLoading && <p className="front-extensions-empty">{t('extensions.installed.loading')}</p>}
        {hasError && <p className="front-extensions-empty">{t('extensions.installed.error')}</p>}
        {!isLoading && !hasError && visiblePlugins.map((plugin, index) => (
          <article className="front-plugin-row" key={plugin.id}>
            <div className="front-plugin-index">{String(index + 1).padStart(2, '0')}</div>
            <div className="front-plugin-mark">{(plugin.label || plugin.id).slice(0, 1)}</div>
            <div className="front-plugin-main">
              <div className="front-plugin-heading-row"><h2>{plugin.label || plugin.id}</h2><span className="front-plugin-badge">{plugin.version}</span></div>
              <p>{plugin.description || t('extensions.installed.noDescription')}</p>
              <div className="front-plugin-meta">{plugin.contributions.map((contribution) => <span key={contribution.id}>{contribution.label || contribution.id}</span>)}</div>
              <div className="front-plugin-meta">{plugin.deployedComponents?.length
                ? plugin.deployedComponents.map((component) => <span key={`${component.type}:${component.name}`}>{component.name}: {component.status}</span>)
                : <span>{t('extensions.installed.noComponents')}</span>}</div>
            </div>
            <span className={`front-plugin-health ${plugin.isHealthy ? 'front-plugin-health-ok' : ''}`}>{plugin.isHealthy ? t('extensions.installed.healthy') : t('extensions.installed.attention')}</span>
          </article>
        ))}
        {!isLoading && !hasError && visiblePlugins.length === 0 && <p className="front-extensions-empty">{t('extensions.installed.empty')}</p>}
      </section>
    </div>
  );
}