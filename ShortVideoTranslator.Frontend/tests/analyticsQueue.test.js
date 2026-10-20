import test from 'node:test';
import assert from 'node:assert/strict';
import { EventQueue } from '../src/analyticsQueue.js';
const event = (id, kind = 'view') => ({ id, kind, occurredAt: new Date(100000000).toISOString() });
test('consent gates upload, batch limit and successful removal preserve new events', async () => {
  let items = []; let calls = 0;
  const q = new EventQueue({ read: () => items, write: value => { items = value; }, now: () => 100000000,
    send: async batch => { calls++; assert.equal(batch.length, 25); q.add(event('new')); } });
  for (let i = 0; i < 30; i++) q.add(event(String(i)));
  await q.flush(() => false); assert.equal(calls, 0);
  await q.flush(() => true); assert.equal(items.length, 6); assert.equal(items.at(-1).id, 'new');
});
test('network failure retains stable IDs, backs off and retry is idempotent', async () => {
  let items = []; let now = 100000000; let calls = 0;
  const q = new EventQueue({ read: () => items, write: value => { items = value; }, now: () => now, send: async batch => { calls++; assert.equal(batch[0].id, 'stable'); if (calls === 1) throw new Error('offline'); } });
  q.add(event('stable')); await q.flush(() => true); await q.flush(() => true);
  assert.equal(calls, 1); assert.equal(items.length, 1); now += 60001; await q.flush(() => true); assert.equal(items.length, 0);
});
test('revocation during upload clears queue; only one heartbeat queues and storage is bounded', async () => {
  let items = [];
  const q = new EventQueue({ read: () => items, write: value => { items = value; }, now: () => 100000000, send: async () => q.clear() });
  q.add(event('h1', 'heartbeat')); q.add(event('h2', 'heartbeat')); assert.equal(items.length, 1);
  for (let i = 0; i < 110; i++) q.add(event(String(i))); assert.equal(items.length, 100);
  await q.flush(() => true); assert.equal(items.length, 0);
});
