export function distanceMeters(a, b) {
  const radians = Math.PI / 180;
  const h = Math.sin((b.latitude - a.latitude) * radians / 2) ** 2 + Math.cos(a.latitude * radians) * Math.cos(b.latitude * radians) * Math.sin((b.longitude - a.longitude) * radians / 2) ** 2;
  return 6371000 * 2 * Math.atan2(Math.sqrt(h), Math.sqrt(Math.max(0, 1 - h)));
}

export class GeofenceEngine {
  constructor() { this.pending = new Map(); this.cooldown = new Map(); this.inside = new Set(); }
  update(position, places, now) {
    if (!Number.isFinite(position.latitude) || Math.abs(position.latitude) > 90 || !Number.isFinite(position.longitude) || Math.abs(position.longitude) > 180 || !Number.isFinite(position.accuracy) || position.accuracy < 0 || position.accuracy > 100) {
      this.pending.clear(); return null;
    }
    const eligible = places.filter(place => place.locationVerified && Number.isFinite(place.latitude) && Number.isFinite(place.longitude));
    const candidates = [];
    for (const place of eligible) {
      const distance = distanceMeters(position, place);
      if (distance > (place.triggerRadius || 30)) {
        this.pending.delete(place.id);
        if (this.inside.delete(place.id)) this.cooldown.set(place.id, now + 300000);
        continue;
      }
      if (this.inside.has(place.id) || (this.cooldown.get(place.id) || 0) > now) continue;
      if (!this.pending.has(place.id)) this.pending.set(place.id, now);
      if (now - this.pending.get(place.id) >= 3000) candidates.push({ place, distance });
    }
    candidates.sort((a, b) => (b.place.audioPriority || 0) - (a.place.audioPriority || 0) || a.distance - b.distance);
    if (!candidates.length) return null;
    const selected = candidates[0].place;
    this.inside.add(selected.id); this.pending.delete(selected.id); return selected;
  }
}

export function nearbyPlaces(position, places, radius = 1000) {
  if (!position || !Number.isFinite(position.latitude) || Math.abs(position.latitude) > 90 ||
    !Number.isFinite(position.longitude) || Math.abs(position.longitude) > 180 ||
    !Number.isFinite(position.accuracy) || position.accuracy < 0 || position.accuracy > 100) return [];
  return places.filter(p => p.locationVerified && Number.isFinite(p.latitude) && Math.abs(p.latitude) <= 90 && Number.isFinite(p.longitude) && Math.abs(p.longitude) <= 180)
    .map(place => ({ place, distance: distanceMeters(position, place) }))
    .filter(item => item.distance <= radius).sort((a, b) => a.distance - b.distance || a.place.id - b.place.id);
}

export function nearbyHotset(position, places, radius = 1500, limit = 10) {
  return places.filter(p => p.locationVerified && Number.isFinite(p.latitude) && Number.isFinite(p.longitude))
    .map(place => ({ place, distance: distanceMeters(position, place) }))
    .filter(item => item.distance <= radius).sort((a, b) => a.distance - b.distance).slice(0, limit).map(item => item.place);
}
