import test from 'node:test';
import assert from 'node:assert/strict';
import { gomCumDiaDiem } from "../src/gomCumBanDo.js";
const places = [1, 2, 3].map((id) => ({ id, locationVerified: true, latitude: 10, longitude: 106 + id / 1000 }));
const project = p => ({ x: (p.longitude - 106) * 10000, y: p.latitude });
test('nearby pins cluster at low zoom and separate at street zoom', () => {
  assert.equal(gomCumDiaDiem(places, project, 13, null).length, 1);
  assert.equal(gomCumDiaDiem(places, project, 17, null).length, 3);
  assert.equal(gomCumDiaDiem(places, p => ({ x: p.id * 100, y: 0 }), 13, null).length, 3);
});
test('selected pin stays accessible and grouping is stable across input order', () => {
  const result = gomCumDiaDiem(places, project, 13, 2);
  assert.equal(result.length, 2);
  assert.deepEqual(result.find(g => g.selected).places.map(p => p.id), [2]);
  assert.deepEqual(result, gomCumDiaDiem([...places].reverse(), project, 13, 2));
});
test('unverified or invalid coordinates never produce pins', () => {
  assert.equal(gomCumDiaDiem([...places, { id: 4, latitude: 91, longitude: 106, locationVerified: true }, { id: 5, latitude: 10, longitude: 106 }], project, 17).length, 3);
});
