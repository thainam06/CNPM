// Same source + language + published revision convention as NarrationAssetService.KeyFor.
export async function taoKhoaThuyetMinh(text, language, revision = '') {
  const source = revision ? `v2:${language}:${revision}:${text}` : `v1:${language}:${text}`;
  const digest = await crypto.subtle.digest('SHA-256', new TextEncoder().encode(source));
  return Array.from(new Uint8Array(digest), byte => byte.toString(16).padStart(2, '0')).join('');
}
