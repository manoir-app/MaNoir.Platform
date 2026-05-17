import i18n from 'i18next';
import { initReactI18next } from 'react-i18next';
import type { AppLocale } from './resources';
import { resources } from './resources';

const languageStorageKey = 'manoir.core-admin-ui.language';
const supportedLanguages: AppLocale[] = ['fr', 'en'];

export function resolveSupportedLanguage(languageId?: string | null): AppLocale | null {
  const normalizedLanguageId = languageId?.trim().toLowerCase();
  if (!normalizedLanguageId) {
    return null;
  }

  if (normalizedLanguageId.startsWith('fr')) {
    return 'fr';
  }

  if (normalizedLanguageId.startsWith('en')) {
    return 'en';
  }

  return null;
}

function resolveInitialLanguage(): AppLocale {
  const storedLanguage = globalThis.localStorage?.getItem(languageStorageKey);
  if (storedLanguage && supportedLanguages.includes(storedLanguage as AppLocale)) {
    return storedLanguage as AppLocale;
  }

  const browserLanguage = globalThis.navigator?.language?.toLowerCase() ?? 'fr';
  const resolvedBrowserLanguage = resolveSupportedLanguage(browserLanguage);
  if (resolvedBrowserLanguage) {
    return resolvedBrowserLanguage;
  }

  return 'fr';
}

void i18n
  .use(initReactI18next)
  .init({
    defaultNS: 'translation',
    fallbackLng: 'fr',
    interpolation: {
      escapeValue: false,
    },
    lng: resolveInitialLanguage(),
    resources,
    supportedLngs: supportedLanguages,
  });

i18n.on('languageChanged', (language) => {
  if (supportedLanguages.includes(language as AppLocale)) {
    globalThis.localStorage?.setItem(languageStorageKey, language);
  }
});

export default i18n;