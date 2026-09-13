import { useEffect, useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { catalogDomains, pluginCatalog, type CatalogPlugin } from '../data/pluginCatalog';
import { getInstalledPlugins, getPluginInstallationStatus, installPlugin, validatePluginRepository, type PluginRepositoryValidationModel } from '../lib/api';

export function PluginCatalogPage() {
  const { t } = useTranslation();
  const [activeDomain, setActiveDomain] = useState('all');
  const [query, setQuery] = useState('');
  const [selectedPlugin, setSelectedPlugin] = useState<CatalogPlugin | null>(null);
  const [installedPluginKeys, setInstalledPluginKeys] = useState<Set<string>>(new Set());
  const [repositoryValidation, setRepositoryValidation] = useState<PluginRepositoryValidationModel | null>(null);
  const [isValidatingRepository, setIsValidatingRepository] = useState(false);
  const [isInstallingPlugin, setIsInstallingPlugin] = useState(false);
  const [installationMessage, setInstallationMessage] = useState<string | null>(null);
  const [installationMessageKind, setInstallationMessageKind] = useState<'success' | 'error' | null>(null);

  useEffect(() => {
    let isCancelled = false;
    void getInstalledPlugins()
      .then((plugins) => {
        if (!isCancelled) {
          setInstalledPluginKeys(new Set(plugins.map((plugin) => {
            const repositoryUrl = plugin.repositoryUrl?.trim().replace(/\/+$/, '').toLocaleLowerCase();
            return repositoryUrl || plugin.id.trim().toLocaleLowerCase();
          })));
        }
      })
      .catch(() => {
        // The catalog remains usable when the installed-plugin endpoint is unavailable.
      });
    return () => { isCancelled = true; };
  }, []);

  useEffect(() => {
    const repositoryUrl = selectedPlugin?.repositoryUrl?.trim();
    if (!repositoryUrl) {
      setRepositoryValidation(null);
      setIsValidatingRepository(false);
      return;
    }

    let isCancelled = false;
    setRepositoryValidation(null);
    setIsValidatingRepository(true);
    void validatePluginRepository(repositoryUrl)
      .then((result) => {
        if (!isCancelled) setRepositoryValidation(result);
      })
      .catch(() => {
        if (!isCancelled) {
          setRepositoryValidation({
            repositoryUrl,
            status: 'unavailable',
            exists: false,
            isOfficial: false,
            isSupported: false,
            canInstall: false,
            message: null,
          });
        }
      })
      .finally(() => {
        if (!isCancelled) setIsValidatingRepository(false);
      });

    return () => { isCancelled = true; };
  }, [selectedPlugin]);

  const visiblePlugins = useMemo(() => {
    const normalizedQuery = query.trim().toLocaleLowerCase();
    return pluginCatalog.filter((plugin) => {
      const matchesDomain = activeDomain === 'all' || plugin.domain === activeDomain;
      const searchableText = [plugin.displayName, plugin.description, ...plugin.tags].join(' ').toLocaleLowerCase();
      return matchesDomain && (!normalizedQuery || searchableText.includes(normalizedQuery));
    });
  }, [activeDomain, query]);

  async function handleInstallPlugin() {
    const repositoryUrl = selectedPlugin?.repositoryUrl?.trim();
    if (!repositoryUrl || !repositoryValidation?.canInstall) return;

    setIsInstallingPlugin(true);
    setInstallationMessage(null);
    setInstallationMessageKind(null);
    try {
      const response = await installPlugin(repositoryUrl);
      setInstallationMessage(response.message ?? t('extensions.catalog.installDialog.accepted'));
      setInstallationMessageKind('success');
      if (!response.operationId) return;

      for (let attempt = 0; attempt < 30; attempt += 1) {
        const status = await getPluginInstallationStatus(response.operationId);
        setInstallationMessage(status.message ?? status.step ?? response.message ?? t('extensions.catalog.installDialog.accepted'));
        if (status.status === 'completed') {
          const installedPlugins = await getInstalledPlugins();
          setInstalledPluginKeys(new Set(installedPlugins.map((plugin) => {
            const installedRepositoryUrl = plugin.repositoryUrl?.trim().replace(/\/+$/, '').toLocaleLowerCase();
            return installedRepositoryUrl || plugin.id.trim().toLocaleLowerCase();
          })));
          setSelectedPlugin(null);
          return;
        }
        if (status.status === 'failed') {
          throw new Error(status.message ?? t('extensions.catalog.installDialog.failed'));
        }

        await new Promise((resolve) => window.setTimeout(resolve, 1000));
      }

      throw new Error(t('extensions.catalog.installDialog.timeout'));
    } catch (error) {
      setInstallationMessage(error instanceof Error ? error.message : t('extensions.catalog.installDialog.failed'));
      setInstallationMessageKind('error');
    } finally {
      setIsInstallingPlugin(false);
    }
  }

  return (
    <div className="front-extensions-page">
      <section className="front-extensions-hero">
        <div className="front-login-page-eyebrow">{t('extensions.catalog.eyebrow')}</div>
        <h1 className="front-extensions-title">{t('extensions.catalog.title')}</h1>
        <p className="front-extensions-copy">{t('extensions.catalog.description')}</p>
      </section>

      <section className="front-extensions-toolbar" aria-label={t('extensions.catalog.filtersLabel')}>
        <label className="front-extensions-search">
          <span className="front-sr-only">{t('extensions.catalog.searchLabel')}</span>
          <input
            type="search"
            value={query}
            onChange={(event) => setQuery(event.target.value)}
            placeholder={t('extensions.catalog.searchPlaceholder')}
          />
        </label>
        <div className="front-extensions-tabs" role="tablist" aria-label={t('extensions.catalog.domainsLabel')}>
          {catalogDomains.map((domain) => (
            <button
              className={`front-extensions-tab ${activeDomain === domain ? 'front-extensions-tab-active' : ''}`}
              key={domain}
              onClick={() => setActiveDomain(domain)}
              role="tab"
              aria-selected={activeDomain === domain}
              type="button"
            >
              {t(`extensions.domains.${domain}` as never)}
              <span>{domain === 'all' ? pluginCatalog.length : pluginCatalog.filter((plugin) => plugin.domain === domain).length}</span>
            </button>
          ))}
        </div>
      </section>

      <section className="front-plugin-list" aria-live="polite">
        {installationMessage && !selectedPlugin && <p className="front-plugin-repository-status">{installationMessage}</p>}
        {visiblePlugins.map((plugin, index) => {
          const catalogKey = plugin.repositoryUrl?.trim().replace(/\/+$/, '').toLocaleLowerCase();
          const isInstalled = Boolean(catalogKey && installedPluginKeys.has(catalogKey)) || installedPluginKeys.has(plugin.id.toLocaleLowerCase());
          const hasRepositoryUrl = Boolean(plugin.repositoryUrl?.trim());
          return (
            <article className="front-plugin-row" key={plugin.id}>
              <div className="front-plugin-index">{String(index + 1).padStart(2, '0')}</div>
              <div className="front-plugin-mark">{plugin.displayName.slice(0, 1)}</div>
              <div className="front-plugin-main">
                <div className="front-plugin-heading-row">
                  <h2>{plugin.displayName}</h2>
                  <span className="front-plugin-badge">{isInstalled ? t('extensions.catalog.installed') : t('extensions.catalog.available')}</span>
                </div>
                <p>{plugin.description}</p>
                <div className="front-plugin-meta">
                  <span>{t(`extensions.domains.${plugin.domain}` as never)}</span>
                  <span>{t(`extensions.categories.${plugin.category}` as never)}</span>
                  <span>{plugin.version}</span>
                </div>
              </div>
              {isInstalled ? (
                <span className="front-plugin-action-note">{t('extensions.catalog.installed')}</span>
              ) : hasRepositoryUrl ? (
                <button className="front-button front-button-small front-button-primary" type="button" onClick={() => setSelectedPlugin(plugin)}>
                  {t('extensions.catalog.install')}
                </button>
              ) : (
                <span className="front-plugin-action-note">{t('extensions.catalog.noRepositoryUrl')}</span>
              )}
            </article>
          );
        })}
        {visiblePlugins.length === 0 && <p className="front-extensions-empty">{t('extensions.catalog.empty')}</p>}
      </section>

      {selectedPlugin && (
        <div className="front-plugin-dialog-backdrop" role="presentation" onMouseDown={() => !isInstallingPlugin && setSelectedPlugin(null)}>
          <section className="front-plugin-dialog" role="dialog" aria-modal="true" aria-labelledby="plugin-dialog-title" onMouseDown={(event) => event.stopPropagation()}>
            <div className="front-login-page-eyebrow">{t('extensions.catalog.installDialog.eyebrow')}</div>
            <h2 id="plugin-dialog-title">{selectedPlugin.displayName}</h2>
            <p>{selectedPlugin.description}</p>
            {isValidatingRepository && <p className="front-plugin-repository-status">{t('extensions.catalog.installDialog.checkingRepository')}</p>}
            {!isValidatingRepository && repositoryValidation?.status === 'official' && <p className="front-plugin-repository-status front-plugin-repository-status-official">{t('extensions.catalog.installDialog.officialBadge')}</p>}
            {!isValidatingRepository && repositoryValidation?.status === 'nonOfficial' && (
              <div className="front-plugin-repository-warning">
                <strong>{t('extensions.catalog.installDialog.nonOfficialBadge')}</strong>
                <span>{t('extensions.catalog.installDialog.nonOfficialWarning')}</span>
              </div>
            )}
            {!isValidatingRepository && repositoryValidation?.status === 'unsupported' && (
              <div className="front-plugin-repository-warning">
                <strong>{t('extensions.catalog.installDialog.unsupportedBadge')}</strong>
                <span>{t('extensions.catalog.installDialog.unsupportedWarning')}</span>
              </div>
            )}
            {!isValidatingRepository && repositoryValidation?.status === 'notFound' && <p className="front-plugin-repository-warning">{t('extensions.catalog.installDialog.notFound')}</p>}
            {!isValidatingRepository && repositoryValidation?.status === 'unavailable' && <p className="front-plugin-repository-warning">{t('extensions.catalog.installDialog.unavailable')}</p>}
            {installationMessage && <p className={`front-plugin-repository-status front-plugin-repository-status-${installationMessageKind ?? 'success'}`} role="status">{installationMessage}</p>}
            <dl>
              <div><dt>{t('extensions.catalog.installDialog.publisher')}</dt><dd>{selectedPlugin.publisher}</dd></div>
              <div><dt>{t('extensions.catalog.installDialog.version')}</dt><dd>{selectedPlugin.version}</dd></div>
              <div><dt>{t('extensions.catalog.installDialog.repository')}</dt><dd>{selectedPlugin.repositoryUrl ? <a href={selectedPlugin.repositoryUrl} target="_blank" rel="noreferrer">{selectedPlugin.repositoryUrl}</a> : t('extensions.catalog.noRepositoryUrl')}</dd></div>
            </dl>
            <div className="front-plugin-dialog-actions">
              <button className="front-button front-button-large front-button-secondary" type="button" onClick={() => setSelectedPlugin(null)}>{t('placesPage.dialogs.close')}</button>
              <button className="front-button front-button-large front-button-primary" type="button" disabled={!repositoryValidation?.canInstall || isInstallingPlugin} onClick={() => void handleInstallPlugin()}>{isInstallingPlugin ? t('extensions.catalog.installDialog.installing') : t('extensions.catalog.installDialog.install')}</button>
            </div>
          </section>
        </div>
      )}
    </div>
  );
}