import bundles from './ui-bundles.json';
export const chuGiaoDien = (language, key) => bundles[language]?.[key] ?? bundles.en[key] ?? key;
export const goiGiaoDienSanSang = (language) => Object.hasOwn(bundles, language);
