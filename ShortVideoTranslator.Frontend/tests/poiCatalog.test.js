import test from 'node:test';
import assert from 'node:assert/strict';
import { locDiaDiem, trangThaiDiaDiem } from "../src/utils/danhMucDiaDiem.js";
const places = [
  { id: 1, name: 'Nhà thờ Đức Bà', category: 'Kiến trúc', address: 'Công xã Paris', isActive: true },
  { id: 2, name: 'Công viên Tao Đàn', category: 'Không gian', isActive: false },
  { id: 3, name: 'Chợ', category: 'Kiến trúc', activationRequested: true },
  { id: 4, name: 'Lưu trữ', region: 'district4', isActive: true },
];
test('POI search matches Vietnamese names without accents, addresses and IDs', () => {
  assert.equal(locDiaDiem(places, 'duc ba')[0].id, 1);
  assert.equal(locDiaDiem(places, ' cong xa paris ')[0].id, 1);
  assert.equal(locDiaDiem(places, '2')[0].id, 2);
  assert.equal(locDiaDiem(places, 'missing').length, 0);
});
test('POI status distinguishes pending publication and archived data; filters combine', () => {
  assert.deepEqual(places.map(trangThaiDiaDiem), ['public', 'hidden', 'pending', 'archived']);
  assert.deepEqual(locDiaDiem(places, '', 'public', 'Kiến trúc').map(p => p.id), [1]);
  assert.deepEqual(locDiaDiem(places, '', 'pending').map(p => p.id), [3]);
  assert.equal(places.length, 4);
});
