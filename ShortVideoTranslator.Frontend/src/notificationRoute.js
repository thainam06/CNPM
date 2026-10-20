export function notificationRoute(hash) {
  if (hash === '#thong-bao') return { inbox: true, id: null, invalid: false };
  if (!hash.startsWith('#thong-bao/')) return { inbox: false, id: null, invalid: false };
  try {
    const id = decodeURIComponent(hash.slice('#thong-bao/'.length));
    return /^[a-zA-Z0-9_-]{1,128}$/.test(id) ? { inbox: true, id, invalid: false } : { inbox: true, id: null, invalid: true };
  } catch { return { inbox: true, id: null, invalid: true }; }
}
