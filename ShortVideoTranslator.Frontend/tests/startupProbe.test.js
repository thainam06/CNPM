import test from 'node:test';
import assert from 'node:assert/strict';
import { startupProbe } from '../src/startupProbe.js';
test('startup probes retry once and do not report HTTP or JSON errors as ready', async () => {
  let calls = 0;
  assert.equal(await startupProbe('/health', undefined, async () => { calls++; return new Response(JSON.stringify({ status: calls === 2 ? 'ready' : 'not-ready' })); }), 'ready');
  assert.equal(calls, 2);
  assert.equal(await startupProbe('/health', undefined, async () => new Response('bad json')), 'unavailable');
  assert.equal(await startupProbe('/health', undefined, async () => new Response('{}', { status: 503 })), 'unavailable');
});
test('unavailable network degrades independently; cancellation prevents subsequent probes', async () => {
  let calls = 0;
  assert.equal(await startupProbe('/health', undefined, async () => { calls++; throw new Error('offline'); }), 'unavailable');
  assert.equal(calls, 2);
  const controller = new AbortController();
  await assert.rejects(() => startupProbe('/health', controller.signal, async () => { controller.abort(); throw new Error('cancelled'); }), { name: 'AbortError' });
});
