import { test } from 'node:test';
import assert from 'node:assert/strict';
import { normalizeTriggerRadius } from '../src/triggerRadius.js';

test('trigger radius cannot be negative or below the 10-meter minimum', () => {
  assert.equal(normalizeTriggerRadius(-1), 10);
  assert.equal(normalizeTriggerRadius('-25'), 10);
  assert.equal(normalizeTriggerRadius(0), 10);
  assert.equal(normalizeTriggerRadius(9), 10);
});

test('trigger radius stays within the supported range and preserves empty input', () => {
  assert.equal(normalizeTriggerRadius(10), 10);
  assert.equal(normalizeTriggerRadius(30), 30);
  assert.equal(normalizeTriggerRadius(1000), 1000);
  assert.equal(normalizeTriggerRadius(1001), 1000);
  assert.equal(normalizeTriggerRadius(''), '');
  assert.equal(normalizeTriggerRadius('invalid'), '');
});
