import { test } from 'node:test';
import assert from 'node:assert/strict';
import { chuanHoaBanKinhKichHoat } from "../src/banKinhKichHoat.js";

test('trigger radius cannot be negative or below the 10-meter minimum', () => {
  assert.equal(chuanHoaBanKinhKichHoat(-1), 10);
  assert.equal(chuanHoaBanKinhKichHoat('-25'), 10);
  assert.equal(chuanHoaBanKinhKichHoat(0), 10);
  assert.equal(chuanHoaBanKinhKichHoat(9), 10);
});

test('trigger radius stays within the supported range and preserves empty input', () => {
  assert.equal(chuanHoaBanKinhKichHoat(10), 10);
  assert.equal(chuanHoaBanKinhKichHoat(30), 30);
  assert.equal(chuanHoaBanKinhKichHoat(1000), 1000);
  assert.equal(chuanHoaBanKinhKichHoat(1001), 1000);
  assert.equal(chuanHoaBanKinhKichHoat(''), '');
  assert.equal(chuanHoaBanKinhKichHoat('invalid'), '');
});
