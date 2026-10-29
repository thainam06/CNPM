import { test } from 'node:test';
import assert from 'node:assert/strict';
import { startPresenceTracking } from '../src/presenceTracker.js';
test('page exit leaves immediately; resume uses a fresh session; cleanup removes listeners', async () => {
  const handlers = {}; const heartbeats = []; const leaves = []; let visible = true; let sequence = 0; let timer; let removed = 0;
  const subscribe = name => handler => { handlers[name] = handler; return () => { removed++; delete handlers[name]; }; };
  const stop = startPresenceTracking({ createSession: () => `session-${++sequence}`, sendHeartbeat: async id => heartbeats.push(id), sendLeave: id => leaves.push(id), isVisible: () => visible,
    subscribeVisibility: subscribe('visibility'), subscribePageHide: subscribe('hide'), subscribePageShow: subscribe('show'), subscribeOnline: subscribe('online'),
    schedule: callback => { timer = callback; return 1; }, cancel: () => { timer = null; } });
  await Promise.resolve(); assert.deepEqual(heartbeats, ['session-1']);
  visible = false; handlers.visibility(); handlers.hide(); assert.deepEqual(leaves, ['session-1']); assert.equal(timer, null);
  visible = true; handlers.show(); await Promise.resolve(); assert.deepEqual(heartbeats, ['session-1', 'session-2']);
  stop(); assert.deepEqual(leaves, ['session-1', 'session-2']); assert.equal(removed, 4);
});
test('a delayed heartbeat cannot schedule another upload after leaving', async () => {
  let complete; let hide; let scheduled = 0; let leaves = 0;
  const stop = startPresenceTracking({ createSession: () => 'a', sendHeartbeat: () => new Promise(resolve => { complete = resolve; }), sendLeave: () => { leaves++; }, isVisible: () => true,
    subscribeVisibility: () => () => {}, subscribePageHide: fn => { hide = fn; return () => {}; }, subscribePageShow: () => () => {}, subscribeOnline: () => () => {}, schedule: () => { scheduled++; }, cancel: () => {} });
  hide(); complete(); await Promise.resolve(); assert.equal(scheduled, 0); assert.equal(leaves, 1); stop(); assert.equal(leaves, 1);
});
