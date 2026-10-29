import { useEffect, useState } from 'react';
import { watchLocation } from './locationWatch';

// One watcher is shared by nearby suggestions, geofencing and opt-in location sharing.
export function useNearbyLocation() {
  const [enabled, setEnabled] = useState(true);
  const [position, setPosition] = useState(null);
  const [error, setError] = useState('');
  useEffect(() => {
    if (!enabled) return;
    const stop = watchLocation({ geolocation: navigator.geolocation, secure: window.isSecureContext,
      onPosition: value => { setPosition({ ...value, timestamp: value.observedAt }); setError(''); },
      onError: message => { setError(message); setPosition(null); setEnabled(false); }
    });
    const timer = setInterval(() => setPosition(previous => previous && Date.now() - previous.observedAt >= 30000 ? null : previous), 5000);
    return () => { stop(); clearInterval(timer); };
  }, [enabled]);
  function start() { setError(''); setPosition(null); setEnabled(true); }
  function clear() { setEnabled(false); setPosition(null); setError(''); }
  return { enabled, position, error, start, clear };
}
