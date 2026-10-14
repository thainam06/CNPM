import test from 'node:test';
import assert from 'node:assert/strict';
import { readFile } from 'node:fs/promises';
import vm from 'node:vm';

const source = await readFile(new URL('../public/sw.js', import.meta.url), 'utf8');
async function workerFixture() {
  const origin = 'http://localhost:5173';
  const listeners = new Map(), stores = new Map();
  let online = true;
  const urlOf = request => new URL(typeof request === 'string' ? request : request.url, origin).href;
  class Cache {
    records = new Map();
    async addAll(urls) {
      for (const url of urls) this.records.set(urlOf(url), { response: new Response(url === '/' ? 'installed build html' : 'cached public asset', { headers: { Vary: 'Origin' } }), origin: '' });
    }
    async match(request, options = {}) {
      const entry = this.records.get(urlOf(request));
      if (!entry) return undefined;
      const requestOrigin = typeof request === 'string' ? '' : request.headers?.get('Origin') || '';
      if (!options.ignoreVary && entry.response.headers.get('Vary') === 'Origin' && requestOrigin !== entry.origin) return undefined;
      return entry.response.clone();
    }
    async put(request, response) { this.records.set(urlOf(request), { response, origin: '' }); }
    async keys() { return [...this.records.keys()].map(url => new Request(url)); }
    async delete(request) { return this.records.delete(urlOf(request)); }
  }
  const caches = {
    async open(name) { if (!stores.has(name)) stores.set(name, new Cache()); return stores.get(name); },
    async keys() { return [...stores.keys()]; },
    async delete(name) { return stores.delete(name); },
  };
  const self = { __BUILD_ID: 'test-build', __PRECACHE: ['/assets/app.js', '/assets/map-worker.js'], location: { origin },
    clients: { claim: async () => {}, get: async () => null }, skipWaiting() {}, addEventListener: (type, callback) => listeners.set(type, callback) };
  vm.runInNewContext(source, { self, caches, URL, Promise, fetch: async () => { if (!online) throw new TypeError('offline'); return new Response('new network build html'); } });
  let install;
  listeners.get('install')({ waitUntil: promise => { install = promise; } }); await install;
  async function fetchRequest(path, mode = 'cors') {
    let response, pending = [];
    listeners.get('fetch')({ request: { url: origin + path, method: 'GET', mode, headers: new Headers({ Origin: origin }) },
      respondWith: promise => { response = promise; }, waitUntil: promise => pending.push(promise) });
    const result = response ? await response : undefined; await Promise.all(pending); return result;
  }
  return { fetchRequest, caches, stores, offline: () => { online = false; } };
}
test('offline module and worker requests survive Vary: Origin from Vite', async () => {
  const worker = await workerFixture(); worker.offline();
  assert.equal(await (await worker.fetchRequest('/assets/app.js')).text(), 'cached public asset');
  assert.equal(await (await worker.fetchRequest('/assets/map-worker.js')).text(), 'cached public asset');
});
test('new network HTML cannot replace installed build HTML in the old shell', async () => {
  const worker = await workerFixture();
  assert.equal(await (await worker.fetchRequest('/', 'navigate')).text(), 'new network build html');
  worker.offline();
  assert.equal(await (await worker.fetchRequest('/', 'navigate')).text(), 'installed build html');
});
test('private API, admin and owner requests are never served from shell cache', async () => {
  const worker = await workerFixture(); worker.offline();
  assert.equal(await worker.fetchRequest('/api/admin/users'), undefined);
  assert.equal(await worker.fetchRequest('/admin', 'navigate'), undefined);
  assert.equal(await worker.fetchRequest('/owner', 'navigate'), undefined);
});
