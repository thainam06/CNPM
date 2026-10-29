import test from 'node:test';
import assert from 'node:assert/strict';
import { watchLocation, validPosition } from '../src/locationWatch.js';
import { nearbyPlaces, GeofenceEngine } from '../src/geofence.js';

const result = { coords: { latitude: 0, longitude: 0, accuracy: 10 }, timestamp: 100000 };
const flush = () => new Promise(resolve => queueMicrotask(resolve));
test('automatically watches; stop clears watcher and ignores late device callbacks', async () => {
  let success; let requests = 0; let cleared; const updates = [];
  const geolocation = { watchPosition(ok) { success = ok; requests++; return 7; }, clearWatch(id) { cleared = id; } };
  const stop = watchLocation({ geolocation, secure: true, now: () => 100000, onPosition: p => updates.push(p), onError: assert.fail });
  await flush(); assert.equal(requests, 1);
  success(result); assert.equal(updates.length, 1);
  stop(); success(result); assert.equal(updates.length, 1); assert.equal(cleared, 7);
  const unmounted = watchLocation({ geolocation, secure: true, onPosition: assert.fail, onError: assert.fail });
  unmounted(); await flush(); assert.equal(requests, 1);
});
test('permission denial, unavailable GPS, timeout and insecure HTTP have actionable messages', async () => {
  for (const [code, text] of [[1, 'bị từ chối'], [2, 'Không lấy được'], [3, 'chưa trả kết quả']]) {
    let error;
    const stop = watchLocation({ secure: true, geolocation: { watchPosition(ok, fail) { fail({ code }); return 1; }, clearWatch() {} }, onPosition: assert.fail, onError: v => { error = v; } });
    await flush(); assert.ok(error.includes(text)); stop();
  }
  let error;
  watchLocation({ secure: false, geolocation: { watchPosition: assert.fail }, onError: v => { error = v; } });
  await flush(); assert.match(error, /HTTPS/);
});
test('rejects stale/future, invalid coordinates and invalid accuracy', () => {
  assert.ok(validPosition(result, 100000));
  for (const timestamp of [0, 106000, NaN]) assert.ok(!validPosition({ ...result, timestamp }, 100000));
  for (const coords of [{ latitude: 91 }, { longitude: 181 }, { accuracy: -1 }, { accuracy: NaN }])
    assert.ok(!validPosition({ ...result, coords: { ...result.coords, ...coords } }, 100000));
});
test('nearby list filters 1/3/5/10 km and reorders on movement without mutating catalog', () => {
  const places = [0.08, 0.04, 0.02, 0.005].map((latitude, index) => ({ id: index + 1, latitude, longitude: 0, locationVerified: true }));
  const position = result.coords;
  assert.deepEqual([1000, 3000, 5000, 10000].map(radius => nearbyPlaces(position, places, radius).length), [1, 2, 3, 4]);
  assert.deepEqual(nearbyPlaces(position, places, 10000).map(x => x.place.id), [4, 3, 2, 1]);
  assert.equal(nearbyPlaces({ ...position, latitude: 0.08 }, places, 10000)[0].place.id, 1);
  assert.deepEqual(places.map(x => x.id), [1, 2, 3, 4]);
  assert.deepEqual(nearbyPlaces({ ...position, accuracy: 200 }, places), []);
  assert.deepEqual(nearbyPlaces(position, places.map(p => ({ ...p, locationVerified: false }))), []);
});
test('unreliable GPS resets pending geofence entry instead of triggering on recovery', () => {
  const engine = new GeofenceEngine(); const place = { id: 1, ...result.coords, locationVerified: true };
  engine.update(result.coords, [place], 0);
  engine.update({ ...result.coords, accuracy: NaN }, [place], 2500);
  assert.equal(engine.update(result.coords, [place], 5000), null);
  assert.equal(engine.update(result.coords, [place], 8000)?.id, 1);
});
