// Two bounded attempts. Catalog/offline startup stays independent of this status probe.
export async function startupProbe(url, signal, request = fetch) {
  for (let attempt = 0; attempt < 2; attempt++) {
    signal?.throwIfAborted();
    try {
      const response = await request(url, { cache: 'no-store', signal: AbortSignal.any([...(signal ? [signal] : []), AbortSignal.timeout(2500)]) });
      if (!response.ok) continue;
      const body = await response.json();
      if (body.status === 'ready') return 'ready';
    } catch { signal?.throwIfAborted(); }
  }
  return 'unavailable';
}
