export function validPosition(position, now = Date.now()) {
  const c = position?.coords;
  return c && Number.isFinite(c.latitude) && Math.abs(c.latitude) <= 90 &&
    Number.isFinite(c.longitude) && Math.abs(c.longitude) <= 180 &&
    Number.isFinite(c.accuracy) && c.accuracy >= 0 &&
    Number.isFinite(position.timestamp) && now - position.timestamp <= 30000 && position.timestamp <= now + 5000;
}

// Watch locally only. Sending coordinates to the server is a separate opt-in.
export function watchLocation({ geolocation, secure, onPosition, onError, now = Date.now }) {
  let active = true; let id;
  const fail = error => {
    if (!active) return;
    const message = error.code === 1 ? 'Quyền vị trí bị từ chối. Cho phép vị trí trong cài đặt trang rồi bấm bật lại; bạn vẫn có thể chọn địa điểm thủ công.'
      : error.code === 3 ? 'GPS chưa trả kết quả. Kiểm tra định vị trên điện thoại rồi bấm bật lại.'
        : 'Không lấy được vị trí. Kiểm tra GPS và kết nối rồi bấm bật lại.';
    onError(message);
  };
  // Deferring avoids duplicate permission requests during React StrictMode remounts.
  queueMicrotask(() => {
    if (!active) return;
    if (!secure) { onError('Định vị cần HTTPS. Hãy mở link HTTPS Cloudflare; HTTP qua địa chỉ LAN không hỗ trợ GPS.'); return; }
    if (!geolocation) { onError('Trình duyệt không hỗ trợ vị trí. Bạn vẫn có thể chọn địa điểm thủ công.'); return; }
    try {
      id = geolocation.watchPosition(result => {
        if (active && validPosition(result, now())) onPosition({ ...result.coords,
          latitude: result.coords.latitude, longitude: result.coords.longitude,
          accuracy: result.coords.accuracy, observedAt: result.timestamp });
      }, fail, { enableHighAccuracy: true, maximumAge: 10000, timeout: 15000 });
    } catch { fail({ code: 2 }); }
  });
  return () => { active = false; if (id !== undefined) geolocation.clearWatch(id); };
}
