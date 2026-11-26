import { useTranslation } from 'react-i18next';

export type PlaceholderKind =
  | 'meshPlaces'
  | 'houseScreens'
  | 'houseLocal'
  | 'peopleUsers'
  | 'systemAbout'
  | 'extensionsCatalog';

interface NavigationPlaceholderPageProps {
  kind: PlaceholderKind;
}

export function NavigationPlaceholderPage({ kind }: NavigationPlaceholderPageProps) {
  const { t } = useTranslation();

  return (
    <section className="front-placeholder-surface">
      <div className="front-login-page-eyebrow">{t(`placeholders.${kind}.eyebrow`)}</div>
      <h1 className="front-placeholder-surface-title">{t(`placeholders.${kind}.title`)}</h1>
      <p className="front-placeholder-surface-copy">{t(`placeholders.${kind}.description`)}</p>
    </section>
  );
}
