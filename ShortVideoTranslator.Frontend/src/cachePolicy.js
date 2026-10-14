// Keep at most three language packs and 300 audio files per language.
export function planAudioCache(entries, incoming, now = Date.now()) {
  const next = { ...entries, [incoming.key]: { language: incoming.language, accessedAt: now } };
  const languageUse = new Map();
  for (const item of Object.values(next)) languageUse.set(item.language, Math.max(languageUse.get(item.language) || 0, item.accessedAt));
  const keep = new Set([...languageUse].sort((a, b) => b[1] - a[1]).slice(0, 3).map(([language]) => language));
  const removed = [];
  for (const language of languageUse.keys()) {
    const files = Object.entries(next).filter(([, item]) => item.language === language).sort((a, b) => b[1].accessedAt - a[1].accessedAt);
    for (const [key] of files.slice(keep.has(language) ? 300 : 0)) { removed.push(key); delete next[key]; }
  }
  return { entries: next, removed };
}
