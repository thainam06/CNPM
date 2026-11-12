export function normalizeTriggerRadius(value) {
  if (value === '') return '';
  const radius = Number(value);
  if (!Number.isFinite(radius)) return '';
  return Math.min(1000, Math.max(10, radius));
}
