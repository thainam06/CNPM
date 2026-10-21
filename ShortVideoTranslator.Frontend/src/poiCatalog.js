export const normalizePoi = value => String(value ?? '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
export const poiStatus = place => place.region === 'district4' ? 'archived' : place.isActive ? 'public' : place.activationRequested ? 'pending' : 'hidden';
export function filterPois(places, query, status = '', category = '') {
  const search = normalizePoi(query).trim();
  return places.filter(place => (!status || poiStatus(place) === status) && (!category || place.category === category) &&
    normalizePoi(`${place.id} ${place.name} ${place.category} ${place.address || ''}`).includes(search));
}
