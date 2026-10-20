// Group nearby projected screen points; selected POIs remain individually accessible.
// At street-level zoom every place remains selectable, including coincident coordinates.
export function clusterPlaces(places, project, zoom, selectedId, radius = 60) {
  const groups = [];
  const valid = places.filter(p => p.locationVerified && Number.isFinite(p.latitude) && Math.abs(p.latitude) <= 90 && Number.isFinite(p.longitude) && Math.abs(p.longitude) <= 180);
  for (const place of [...valid].sort((a, b) => String(a.id).localeCompare(String(b.id)))) {
    const point = project(place);
    if (!Number.isFinite(point.x) || !Number.isFinite(point.y)) continue;
    const group = zoom < 17 && place.id !== selectedId
      ? groups.find(g => !g.selected && Math.hypot(g.x - point.x, g.y - point.y) < radius) : null;
    if (group) {
      const n = group.places.length;
      group.x = (group.x * n + point.x) / (n + 1); group.y = (group.y * n + point.y) / (n + 1);
      group.latitude = (group.latitude * n + place.latitude) / (n + 1);
      group.longitude = (group.longitude * n + place.longitude) / (n + 1);
      group.places.push(place);
    } else groups.push({ x: point.x, y: point.y, latitude: place.latitude, longitude: place.longitude, selected: place.id === selectedId, places: [place] });
  }
  return groups;
}
