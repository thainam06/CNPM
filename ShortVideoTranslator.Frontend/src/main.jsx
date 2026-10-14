import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.jsx'
import Portal from './Portal.jsx'

createRoot(document.getElementById('root')).render(
  <StrictMode>
    {window.location.pathname.startsWith('/admin') || window.location.pathname.startsWith('/owner') ? <Portal /> : <App />}
  </StrictMode>,
)

// Register only production assets: Vite HMR modules are unsuitable for offline caching.
if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js').catch(() => {}));
}
