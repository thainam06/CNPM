export const chuanHoaTuKhoa = value => String(value ?? '').toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');
export const trangThaiDiaDiem = place => place.region === 'district4' ? 'archived' : place.isActive ? 'public' : place.activationRequested ? 'pending' : 'hidden';
export function locDiaDiem(places, query, status = '', category = '') {
  const search = chuanHoaTuKhoa(query).trim();
  return places.filter(place => (!status || trangThaiDiaDiem(place) === status) && (!category || place.category === category) &&
    chuanHoaTuKhoa(`${place.id} ${place.name} ${place.category} ${place.address || ''}`).includes(search));
}
