import { useTranslation } from 'react-i18next';
import { useConsoleHomePageViewModel } from '../hooks/useConsoleHomePageViewModel';

export function ConsoleHomePage() {
  const { t } = useTranslation();
  const { primaryStats, workstreams } = useConsoleHomePageViewModel();

  return (
    <div className="front-console-page">
      <section className="front-console-hero">
        <div className="front-login-page-eyebrow">{t('console.eyebrow')}</div>
        <h1 className="front-console-title">{t('console.title')}</h1>
        <p className="front-console-copy">
          {t('console.description')}
        </p>
      </section>

      <section className="front-console-stats-grid" aria-label="Synthese console">
        {primaryStats.map((stat) => (
          <article className="front-console-stat-card" key={stat.label}>
            <div className="front-console-stat-label">{stat.label}</div>
            <div className="front-console-stat-value">{stat.value}</div>
            <div className="front-console-stat-detail">{stat.detail}</div>
          </article>
        ))}
      </section>

      <section className="front-console-section">
        <div className="front-login-page-eyebrow">{t('console.workstreams.eyebrow')}</div>
        <div className="front-console-workstream-grid">
          {workstreams.map((stream) => (
            <article className="front-console-workstream-card" key={stream.title}>
              <h2 className="front-console-workstream-title">{stream.title}</h2>
              <p className="front-console-workstream-copy">{stream.description}</p>
            </article>
          ))}
        </div>
      </section>
    </div>
  );
}