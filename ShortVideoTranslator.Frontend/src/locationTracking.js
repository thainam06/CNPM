export const locationMaxAge = 30000;
export function validLocation(result, now = Date.now()) {
  const point = result?.coords;
  return Number.isFinite(point?.latitude) && Math.abs(point.latitude) <= 90 && Number.isFinite(point?.longitude) && Math.abs(point.longitude) <= 180
    && Number.isFinite(point?.accuracy) && point.accuracy >= 0 && Number.isFinite(result.timestamp)
    && now - result.timestamp <= locationMaxAge && result.timestamp <= now + 5000;
}
// Adapter can be tested without requesting the developer's real location.
export function trackLocation({ geolocation, onPosition, onError, now = Date.now }) {
  let stopped = false;
  let last = -Infinity;
  const id = geolocation.watchPosition(result => {
    if (stopped || !validLocation(result, now()) || now() - last < 5000) return;
    last = now();
    onPosition({ latitude: result.coords.latitude, longitude: result.coords.longitude, accuracy: result.coords.accuracy, timestamp: result.timestamp });
  }, error => { if (!stopped) onError(error); }, { enableHighAccuracy: true, maximumAge: 5000, timeout: 15000 });
  return () => { stopped = true; geolocation.clearWatch(id); };
}
