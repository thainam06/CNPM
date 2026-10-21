import test from 'node:test';
import assert from 'node:assert/strict';
import { filterPois, poiStatus } from '../src/poiCatalog.js';
const places = [
  { id: 1, name: 'Nhà thờ Đức Bà', category: 'Kiến trúc', address: 'Công xã Paris', isActive: true },
  { id: 2, name: 'Công viên Tao Đàn', category: 'Không gian', isActive: false },
  { id: 3, name: 'Chợ', category: 'Kiến trúc', activationRequested: true },
  { id: 4, name: 'Lưu trữ', region: 'district4', isActive: true },
];
test('POI search matches Vietnamese names without accents, addresses and IDs', () => {
  assert.equal(filterPois(places, 'duc ba')[0].id, 1);
  assert.equal(filterPois(places, ' cong xa paris ')[0].id, 1);
  assert.equal(filterPois(places, '2')[0].id, 2);
  assert.equal(filterPois(places, 'missing').length, 0);
});
test('POI status distinguishes pending publication and archived data; filters combine', () => {
  assert.deepEqual(places.map(poiStatus), ['public', 'hidden', 'pending', 'archived']);
  assert.deepEqual(filterPois(places, '', 'public', 'Kiến trúc').map(p => p.id), [1]);
  assert.deepEqual(filterPois(places, '', 'pending').map(p => p.id), [3]);
  assert.equal(places.length, 4);
});
