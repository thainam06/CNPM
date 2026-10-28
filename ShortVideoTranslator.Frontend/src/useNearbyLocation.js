import { useEffect, useState } from 'react';
import { locationMaxAge, trackLocation } from './locationTracking';
import { locationErrorMessage } from './nearbyPlaces.js';

export function useNearbyLocation() {
  // Start on entry; the browser owns the first-use location permission prompt.
  const [enabled, setEnabled] = useState(() => window.isSecureContext && !!navigator.geolocation);
  const [position, setPosition] = useState(null);
  const [error, setError] = useState(() => window.isSecureContext && navigator.geolocation ? '' : 'Định vị cần HTTPS hoặc localhost và trình duyệt hỗ trợ GPS. Hãy mở đường dẫn Cloudflare HTTPS.');
  useEffect(() => {
    if (!enabled) return;
    let stop; let active = true;
    try {
      stop = trackLocation({ geolocation: navigator.geolocation, onPosition: value => { setPosition(value); setError(''); }, onError: caught => {
        setError(locationErrorMessage(caught.code));
        if (caught.code === 1) { setEnabled(false); setPosition(null); }
      } });
    } catch { queueMicrotask(() => { if (active) { setError('Không thể bắt đầu định vị. Hãy kiểm tra quyền vị trí rồi thử lại.'); setEnabled(false); } }); }
    const timer = setInterval(() => setPosition(previous => previous && Date.now() - previous.timestamp > locationMaxAge ? null : previous), 5000);
    return () => { active = false; stop?.(); clearInterval(timer); };
  }, [enabled]);
  function start() {
    setError(''); setPosition(null);
    if (!window.isSecureContext || !navigator.geolocation) { setError('Định vị cần HTTPS hoặc localhost và trình duyệt hỗ trợ GPS. Hãy mở đường dẫn Cloudflare HTTPS.'); return; }
    setEnabled(true);
  }
  function clear() { setEnabled(false); setPosition(null); setError(''); }
  return { enabled, position, error, start, clear };
}
