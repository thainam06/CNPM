import { test } from 'node:test';
import assert from 'node:assert/strict';
import { trackLocation, validLocation } from '../src/locationTracking.js';
import { rankedPlaces } from '../src/nearbyPlaces.js';
test('movement updates nearby recommendations; stop blocks late GPS callbacks', () => {
  let now = 100000; let callback; let failure; let cleared;
  const received = []; const errors = [];
  const geo = { watchPosition(success, error) { callback = success; failure = error; return 7; }, clearWatch(id) { cleared = id; } };
  const stop = trackLocation({ geolocation: geo, now: () => now, onPosition: position => received.push(position), onError: error => errors.push(error.code) });
  const gps = latitude => ({ timestamp: now, coords: { latitude, longitude: 106.7, accuracy: 20 } });
  callback(gps(10.77)); callback(gps(10.78));
  assert.equal(received.length, 1);
  now += 6000; callback(gps(10.78));
  const places = [10.77, 10.78].map((latitude, id) => ({ id, latitude, longitude: 106.7, locationVerified: true }));
  assert.equal(rankedPlaces(received[0], places)[0].place.id, 0);
  assert.equal(rankedPlaces(received[1], places)[0].place.id, 1);
  failure({ code: 1 }); assert.deepEqual(errors, [1]);
  stop(); now += 6000; callback(gps(10.79)); failure({ code: 2 });
  assert.equal(cleared, 7); assert.equal(received.length, 2); assert.deepEqual(errors, [1]);
});
test('old, future and invalid GPS fixes are rejected', () => {
  const fix = { coords: { latitude: 10.77, longitude: 106.7, accuracy: 20 }, timestamp: 100000 };
  assert.equal(validLocation(fix, 100000), true);
  assert.equal(validLocation(fix, 140000), false);
  assert.equal(validLocation(fix, 90000), false);
  assert.equal(validLocation({ ...fix, coords: { ...fix.coords, accuracy: NaN } }, 100000), false);
});
