import bundles from './ui-bundles.json';
export const uiText = (language, key) => bundles[language]?.[key] ?? bundles.en[key] ?? key;
export const uiBundleReady = (language) => Object.hasOwn(bundles, language);
