import test from 'node:test';
import assert from 'node:assert/strict';
import { chonDiaDiemUuTien } from "../src/vungKichHoat.js";

test('hotset sorts nearby verified POIs and caps prefetch at three', () => {
  const origin = { latitude: 10.76, longitude: 106.7 };
  const places = [1, 2, 3, 4].reverse().map(id => ({ id, latitude: origin.latitude + id * .0001, longitude: origin.longitude, locationVerified: true }));
  places.push({ id: 5, ...origin, locationVerified: false }, { id: 6, latitude: 10.8, longitude: 106.7, locationVerified: true });
  assert.deepEqual(chonDiaDiemUuTien(origin, places, 500, 3).map(p => p.id), [1, 2, 3]);
  assert.deepEqual(chonDiaDiemUuTien(origin, places).map(p => p.id), [1, 2, 3, 4]);
});
