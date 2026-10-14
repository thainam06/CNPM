import test from 'node:test';
import assert from 'node:assert/strict';
import { canShareRuntime } from '../src/runtimePolicy.js';
const request = { consent: true, gps: true, sessionId: '00000000-0000-4000-8000-000000000001', position: { latitude: 10.76, longitude: 106.7, accuracy: 20, observedAt: 1000 }, now: 2000 };
test('GPS and analytics do not grant runtime location consent', () => {
  assert.equal(canShareRuntime({ ...request, consent: false, analyticsConsent: true }), false);
  assert.equal(canShareRuntime({ ...request, gps: false }), false);
  assert.equal(canShareRuntime(request), true);
});
test('stale, future, inaccurate or anonymous-session-free readings cannot be shared', () => {
  assert.equal(canShareRuntime({ ...request, now: 32000 }), false);
  assert.equal(canShareRuntime({ ...request, now: 500 }), false);
  assert.equal(canShareRuntime({ ...request, position: { ...request.position, accuracy: 150 } }), false);
  assert.equal(canShareRuntime({ ...request, sessionId: '' }), false);
});
