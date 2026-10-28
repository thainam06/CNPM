import { test } from 'node:test';
import assert from 'node:assert/strict';
import { rankedPlaces, locationErrorMessage } from '../src/nearbyPlaces.js';
const origin = { latitude: 10.77, longitude: 106.7 };
const point = (id, offset, extra = {}) => ({ id, latitude: origin.latitude + offset, longitude: origin.longitude, locationVerified: true, ...extra });
test('nearby ranks verified POIs by distance and filters radius and invalid coordinates', () => {
  const items = rankedPlaces(origin, [point(2, .01), point(1, .001), point(3, .05), point(4, 0, { locationVerified: false }), point(5, 0, { latitude: null }), point(6, 0, { latitude: 100 })], 2000);
  assert.deepEqual(items.map(item => item.place.id), [1, 2]);
  assert.ok(items[0].distance > 100 && items[0].distance < 120);
});
test('outside service area and invalid GPS produce no invented suggestions', () => {
  assert.deepEqual(rankedPlaces({ latitude: 0, longitude: 0 }, [point(1, 0)], 10000), []);
  assert.deepEqual(rankedPlaces({ latitude: NaN, longitude: 0 }, [point(1, 0)]), []);
});
test('permission denial and timeout have distinct actionable messages', () => {
  assert.match(locationErrorMessage(1), /cấp quyền/);
  assert.match(locationErrorMessage(3), /thời gian/);
  assert.match(locationErrorMessage(2), /GPS/);
});
