import test from 'node:test';
import assert from 'node:assert/strict';
import { notificationRoute } from '../src/notificationRoute.js';
test('notification inbox and detail links survive direct navigation', () => {
  assert.deepEqual(notificationRoute('#thong-bao'), { inbox: true, id: null, invalid: false });
  assert.deepEqual(notificationRoute('#thong-bao/abc123'), { inbox: true, id: 'abc123', invalid: false });
  assert.equal(notificationRoute('#thong-bao/legacy-id_1').id, 'legacy-id_1');
  assert.equal(notificationRoute('#ban-do').inbox, false);
});
test('malformed and path-traversing IDs are never passed to the notification API', () => {
  for (const hash of ['#thong-bao/', '#thong-bao/%', '#thong-bao/%2Fadmin', '#thong-bao/../', '#thong-bao/%00', '#thong-bao/' + 'a'.repeat(129)]) {
    assert.deepEqual(notificationRoute(hash), { inbox: true, id: null, invalid: true });
  }
});
