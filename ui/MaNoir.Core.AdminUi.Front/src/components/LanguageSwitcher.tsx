import { useTranslation } from 'react-i18next';

const languages = ['fr', 'en'] as const;

export function LanguageSwitcher() {
  const { i18n, t } = useTranslation();

  return (
    <div aria-label={t('common.language.label')} className="front-language-switcher" role="group">
      {languages.map((language) => {
        const isActive = i18n.resolvedLanguage === language;

        return (
          <button
            className={`front-language-switcher-button${isActive ? ' front-language-switcher-button-active' : ''}`}
            key={language}
            onClick={() => void i18n.changeLanguage(language)}
            type="button"
          >
            {t(`common.language.${language}`)}
          </button>
        );
      })}
    </div>
  );
}