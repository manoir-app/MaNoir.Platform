import { useTranslation } from 'react-i18next';

type PlaceholderTranslationKey =
  | 'placeholders.meshPlaces.eyebrow'
  | 'placeholders.meshPlaces.title'
  | 'placeholders.meshPlaces.description'
  | 'placeholders.extensionsCatalog.eyebrow'
  | 'placeholders.extensionsCatalog.title'
  | 'placeholders.extensionsCatalog.description';

interface NavigationPlaceholderPageProps {
  eyebrowKey: PlaceholderTranslationKey;
  titleKey: PlaceholderTranslationKey;
  descriptionKey: PlaceholderTranslationKey;
}

export function NavigationPlaceholderPage({ descriptionKey, eyebrowKey, titleKey }: NavigationPlaceholderPageProps) {
  const { t } = useTranslation();

  return (
    <section className="front-placeholder-surface">
      <div className="front-login-page-eyebrow">{t(eyebrowKey)}</div>
      <h1 className="front-placeholder-surface-title">{t(titleKey)}</h1>
      <p className="front-placeholder-surface-copy">{t(descriptionKey)}</p>
    </section>
  );
}