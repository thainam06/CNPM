import { test } from 'node:test';
import assert from 'node:assert/strict';
import { GeofenceEngine, distanceMeters } from '../src/geofence.js';

// Synthetic test coordinates, never inserted into the tourism dataset.
const position = { latitude: 0, longitude: 0, accuracy: 10 };
const place = { id: 1, latitude: 0, longitude: 0, locationVerified: true, triggerRadius: 30, audioPriority: 0 };
test('distance is symmetric and zero at the same point', () => {
  assert.equal(distanceMeters(position, position), 0);
  const other = { latitude: 0.001, longitude: 0 };
  assert.equal(distanceMeters(position, other), distanceMeters(other, position));
  assert.ok(distanceMeters(position, other) > 110 && distanceMeters(position, other) < 112);
});
test('entry debounces, does not repeat inside, and cools down after leaving', () => {
  const engine = new GeofenceEngine();
  assert.equal(engine.update(position, [place], 0), null);
  assert.equal(engine.update(position, [place], 2999), null);
  assert.equal(engine.update(position, [place], 3000)?.id, 1);
  assert.equal(engine.update(position, [place], 10000), null);
  engine.update({ ...position, latitude: 1 }, [place], 11000);
  assert.equal(engine.update(position, [place], 12000), null);
  assert.equal(engine.update(position, [place], 311000), null);
  assert.equal(engine.update(position, [place], 314000)?.id, 1);
});
test('unverified coordinates and inaccurate GPS never trigger narration', () => {
  const engine = new GeofenceEngine();
  assert.equal(engine.update(position, [{ ...place, locationVerified: false }], 0), null);
  assert.equal(engine.update(position, [{ ...place, locationVerified: false }], 5000), null);
  assert.equal(engine.update({ ...position, accuracy: 150 }, [place], 5000), null);
});
test('priority wins for overlapping zones and jitter resets entry debounce', () => {
  const engine = new GeofenceEngine();
  const priority = { ...place, id: 2, audioPriority: 10 };
  engine.update(position, [place, priority], 0);
  assert.equal(engine.update(position, [place, priority], 3000)?.id, 2);
  const jitter = new GeofenceEngine();
  jitter.update(position, [place], 0);
  jitter.update({ ...position, latitude: 1 }, [place], 2000);
  assert.equal(jitter.update(position, [place], 3000), null);
  assert.equal(jitter.update(position, [place], 6000)?.id, 1);
});
