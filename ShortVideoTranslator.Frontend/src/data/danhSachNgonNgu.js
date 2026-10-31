// Shared UI language choices. Keep this order aligned with tuDienQuanTri rows.
export const danhSachNgonNgu = Object.freeze([
  { code: 'vi', name: 'Tiếng Việt' },
  { code: 'en', name: 'English' },
  { code: 'zh', name: '中文' },
  { code: 'ja', name: '日本語' },
  { code: 'ko', name: '한국어' },
  { code: 'fr', name: 'Français' },
].map(language => Object.freeze(language)));

export const maNgonNgu = Object.freeze(danhSachNgonNgu.map(language => language.code));
// Vietnamese is the source text, not a foreign-language translation target.
export const ngonNguBanDich = Object.freeze(danhSachNgonNgu.filter(language => language.code !== 'vi'));
