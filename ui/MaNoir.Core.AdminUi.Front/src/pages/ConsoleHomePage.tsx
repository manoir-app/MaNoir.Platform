import * as React from 'react';
import { useNavigate } from 'react-router';
import { useTranslation } from 'react-i18next';
import { Button } from '@manoir-app/core-admin-ui-kit/button';
import { Card } from '@manoir-app/core-admin-ui-kit/card';
import { PageHeader } from '@manoir-app/core-admin-ui-kit/page-header';
import { StatusPill } from '@manoir-app/core-admin-ui-kit/status-pill';
import { PageIcon } from '../components/PageIcon';
import { useAuthSessionViewModel } from '../hooks/useAuthSessionViewModel';
import { useRegisteredAgentsData } from '../hooks/useRegisteredAgentsData';
import { useAdminNavigationStore } from '../stores/adminNavigationStore';

const sectionIcons: Record<string, string> = {
  house: 'map-pin',
  people: 'users',
  monitoring: 'pulse',
  extensions: 'plug',
  system: 'server',
};

interface AttentionItem {
  id: string;
  title: string;
  detail: string;
  to: string;
}

export function ConsoleHomePage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { currentUser, isServerInfoAvailable, serverLabel, serverUptime, serverVersion } = useAuthSessionViewModel();
  const { summary } = useRegisteredAgentsData();
  const platformMenu = useAdminNavigationStore((state) => state.domainMenus.platform);

  const operator = currentUser?.commonName || currentUser?.firstName || currentUser?.id || t('common.currentUserFallback');

  const attentionItems: AttentionItem[] = [];

  if (!isServerInfoAvailable) {
    attentionItems.push({
      id: 'offline',
      title: t('console.home.attention.offline'),
      detail: t('console.home.attention.offlineDetail'),
      to: '/platform/surveillance/services',
    });
  }

  if (summary.degradedCount > 0) {
    attentionItems.push({
      id: 'degraded',
      title: t('console.home.attention.degraded', { count: summary.degradedCount }),
      detail: t('console.home.attention.degradedDetail'),
      to: '/platform/surveillance/agents',
    });
  }

  if (summary.staleCount > 0) {
    attentionItems.push({
      id: 'stale',
      title: t('console.home.attention.stale', { count: summary.staleCount }),
      detail: t('console.home.attention.staleDetail'),
      to: '/platform/surveillance/agents',
    });
  }

  const installationRows = [
    { label: t('console.home.installation.name'), value: serverLabel },
    { label: t('console.home.installation.version'), value: serverVersion ?? t('common.versionUnavailable') },
    { label: t('console.home.installation.uptime'), value: serverUptime ?? t('common.versionUnavailable') },
    { label: t('console.home.installation.operator'), value: operator },
  ];

  const sectionTiles = (platformMenu?.sections ?? [])
    .filter((section) => section.id !== 'overview' && section.pages.length > 0)
    .map((section) => ({
      id: section.id,
      title: i18nSectionTitle(section.id, section.label, t),
      description: t(`console.home.sections.${section.id}` as never, { defaultValue: '' }) as string,
      href: section.pages[0].href,
      icon: sectionIcons[section.id],
    }));

  return (
    <div className="front-home">
      <PageHeader description={t('console.home.description')} title={t('console.home.title')} variant="page" />

      <div className="front-home-grid">
        <Card className="front-home-card">
          <h2 className="front-home-card-title">{t('console.home.installation.title')}</h2>
          <dl className="front-home-facts">
            {installationRows.map((row) => (
              <div className="front-home-fact" key={row.label}>
                <dt>{row.label}</dt>
                <dd>{row.value}</dd>
              </div>
            ))}
          </dl>
        </Card>

        <Card className="front-home-card" tone={attentionItems.length > 0 ? 'attention' : 'default'}>
          <h2 className="front-home-card-title">{t('console.home.attention.title')}</h2>
          {attentionItems.length === 0 ? (
            <div className="front-home-allgood">
              <StatusPill tone="success">{t('console.home.attention.allGood')}</StatusPill>
              <p>{t('console.home.attention.allGoodDetail')}</p>
            </div>
          ) : (
            <ul className="front-home-attention">
              {attentionItems.map((item) => (
                <li className="front-home-attention-item" key={item.id}>
                  <div className="front-home-attention-text">
                    <strong>{item.title}</strong>
                    <span>{item.detail}</span>
                  </div>
                  <Button
                    onClick={() => {
                      void navigate(item.to);
                    }}
                    size="sm"
                    variant="secondary"
                  >
                    {t('console.home.attention.open')}
                  </Button>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </div>

      {sectionTiles.length > 0 ? (
        <nav aria-label={t('navigation.main')} className="front-home-tiles">
          {sectionTiles.map((tile) => (
            <a
              className="front-home-tile"
              href={tile.href}
              key={tile.id}
              onClick={(event) => {
                event.preventDefault();
                void navigate(tile.href);
              }}
            >
              <span aria-hidden="true" className="front-home-tile-icon">
                <PageIcon kind={tile.icon} />
              </span>
              <span className="front-home-tile-text">
                <strong>{tile.title}</strong>
                <span>{tile.description}</span>
              </span>
            </a>
          ))}
        </nav>
      ) : null}
    </div>
  );
}

function i18nSectionTitle(id: string, fallback: string, t: (key: never) => string) {
  const key = `navigation.sections.${id}`;
  const translated = t(key as never);
  return translated === key ? fallback : translated;
}
