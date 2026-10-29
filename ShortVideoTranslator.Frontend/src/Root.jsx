import { lazy, Suspense } from 'react';
import { PresenceProvider } from './Presence';

const App = lazy(() => import('./App.jsx'));
const Portal = lazy(() => import('./Portal.jsx'));

export default function Root() {
  const portal = window.location.pathname.startsWith('/admin') || window.location.pathname.startsWith('/owner');
  return <PresenceProvider><Suspense fallback={<main role="status" style={{ padding: '32px', color: '#6b452c' }}>Đang mở SmartTour Guide…</main>}>
    {portal ? <Portal /> : <App />}
  </Suspense></PresenceProvider>;
}
