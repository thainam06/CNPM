self.__BUILD_ID = "mvias7tt";
self.__PRECACHE = ["/assets/index-IPlEDVQ9.js","/assets/App-DgA2D3Qi.js","/assets/OfflineMap-P_MXlOtW.js","/assets/PlaceImage-BmGYc7iY.js","/assets/Portal-Dh2gjdi3.js","/assets/App-JVwkZU5g.css","/assets/OfflineMap-DBE96hFo.css","/assets/PlaceImage-cfzI5Bz5.css","/assets/Portal-vARX3dco.css","/assets/index-0Ku1mexT.css","/assets/maplibre-gl-worker-CV6NbyPW.js"];
const SHELL = 'smarttour-shell-' + (self.__BUILD_ID || 'unbuilt');
self.addEventListener('install', event => { event.waitUntil(caches.open(SHELL).then(cache => cache.addAll(['/', '/manifest.webmanifest', '/app-icon.svg', ...(self.__PRECACHE || [])]))); self.skipWaiting(); });
self.addEventListener('activate', event => { event.waitUntil(Promise.all([self.clients.claim(), caches.keys().then(keys => Promise.all(keys.filter(key => key.startsWith('smarttour-shell-') && key !== SHELL).map(key => caches.delete(key))))])); });
self.addEventListener('message', event => { if (event.data?.type === 'GET_BUILD') event.ports[0]?.postMessage({ buildId: self.__BUILD_ID }); });
self.addEventListener('fetch', event => {
  const url = new URL(event.request.url);
  // Cache photos actually viewed, never prefetch OpenStreetMap tiles or private portal data.
  if (event.request.method === 'GET' && event.request.destination === 'image' && url.hostname !== 'tile.openstreetmap.org') {
    event.respondWith((async () => {
      const client = await self.clients.get(event.clientId);
      if (client && /^\/(admin|owner)/.test(new URL(client.url).pathname)) return fetch(event.request);
      const cache = await caches.open('smarttour-public-photos-v1');
      const cached = await cache.match(event.request); if (cached) return cached;
      const response = await fetch(event.request);
      if (response.ok || response.type === 'opaque') {
        await cache.put(event.request, response.clone());
        const keys = await cache.keys(); if (keys.length > 60) await cache.delete(keys[0]);
      }
      return response;
    })()); return;
  }
  if (event.request.method !== 'GET' || url.origin !== self.location.origin || url.pathname.startsWith('/api/') || url.pathname.startsWith('/admin') || url.pathname.startsWith('/owner')) return;
  if (event.request.mode === 'navigate') {
    // Each shell's HTML stays paired with its own hashed JS/CSS/worker assets.
    // Never put a newer build's HTML into an older worker's cache.
    event.respondWith(fetch(event.request).catch(() => caches.open(SHELL).then(cache => cache.match('/', { ignoreVary: true }))));
  } else if (url.pathname.startsWith('/assets/') || ['/manifest.webmanifest', '/app-icon.svg'].includes(url.pathname)) {
    // Public hashed assets are identical for every Origin. Vite sends Vary: Origin;
    // module requests include Origin while install-time cache.addAll requests may not.
    event.respondWith(caches.open(SHELL).then(cache => cache.match(event.request, { ignoreVary: true })).then(cached => cached || fetch(event.request).then(response => { if (response.ok) { const clone = response.clone(); event.waitUntil(caches.open(SHELL).then(cache => cache.put(event.request, clone))); } return response; })));
  }
});
