import { distanceMeters } from './geofence.js';
export function rankedPlaces(position, places, radius = 3000) {
  const valid = point => Number.isFinite(point?.latitude) && Math.abs(point.latitude) <= 90 && Number.isFinite(point?.longitude) && Math.abs(point.longitude) <= 180;
  if (!valid(position)) return [];
  return places.filter(place => place.locationVerified && valid(place))
    .map(place => ({ place, distance: distanceMeters(position, place) }))
    .filter(item => item.distance <= radius)
    .sort((a, b) => a.distance - b.distance || a.place.id - b.place.id);
}
export function locationErrorMessage(code) {
  return code === 1 ? 'Bạn chưa cho phép định vị. Hãy cấp quyền vị trí trong trình duyệt rồi thử lại; vẫn có thể chọn địa điểm thủ công.'
    : code === 3 ? 'Định vị mất quá nhiều thời gian. Hãy bật GPS hoặc thử lại ở nơi thoáng hơn.'
      : 'Chưa lấy được vị trí. Kiểm tra GPS và quyền vị trí của thiết bị rồi thử lại.';
}
