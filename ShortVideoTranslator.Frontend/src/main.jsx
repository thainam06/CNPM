import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import GocUngDung from "./GocUngDung.jsx"

createRoot(document.getElementById('root')).render(
  <StrictMode>
    <GocUngDung />
  </StrictMode>,
)

// Register only production assets: Vite HMR modules are unsuitable for offline caching.
if (import.meta.env.PROD && 'serviceWorker' in navigator) {
  window.addEventListener('load', () => navigator.serviceWorker.register('/sw.js').catch(() => {}));
}
