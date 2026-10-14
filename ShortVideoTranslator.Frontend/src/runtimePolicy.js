export function canShareRuntime({ consent, gps, position, sessionId, now = Date.now() }) {
  return Boolean(consent === true && gps === true && typeof sessionId === 'string' &&
    /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i.test(sessionId) &&
    position && Number.isFinite(position.latitude) && Number.isFinite(position.longitude) &&
    Number.isFinite(position.accuracy) && position.accuracy >= 0 && position.accuracy <= 100 &&
    Number.isFinite(position.observedAt) && now >= position.observedAt && now - position.observedAt <= 30000);
}
