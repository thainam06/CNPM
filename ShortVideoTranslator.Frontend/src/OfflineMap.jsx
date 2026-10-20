import { useEffect, useRef, useState } from 'react';
import * as maplibregl from 'maplibre-gl';
import { FileSource, PMTiles, Protocol } from 'pmtiles';
import 'maplibre-gl/dist/maplibre-gl.css';
import workerUrl from 'maplibre-gl/dist/maplibre-gl-worker.mjs?worker&url';
import { offlineMapStyle } from './mapPack';
import { clusterPlaces } from './mapClusters';

const protocol = new Protocol();
maplibregl.setWorkerUrl(workerUrl);
maplibregl.addProtocol('pmtiles', protocol.tile);

export default function OfflineMap({ pack, places, selectedId, onSelect }) {
  const container = useRef(null);
  const instance = useRef(null);
  const markers = useRef([]);
  const callback = useRef(onSelect);
  const [error, setError] = useState('');
  useEffect(() => { callback.current = onSelect; }, [onSelect]);
  useEffect(() => {
    const source = new FileSource(new File([pack.blob], `${pack.manifest.sha256}.pmtiles`));
    const archive = new PMTiles(source); protocol.add(archive);
    const [west, south, east, north] = pack.manifest.bounds;
    let map;
    try {
      map = new maplibregl.Map({ container: container.current, style: offlineMapStyle(`pmtiles://${source.getKey()}`),
        bounds: [[west, south], [east, north]], fitBoundsOptions: { padding: 24 }, maxBounds: [[west, south], [east, north]],
        minZoom: 12, maxZoom: 18, scrollZoom: false, attributionControl: true });
      map.addControl(new maplibregl.NavigationControl(), 'top-left');
      map.on('error', () => setError('Không đọc được gói bản đồ hoặc WebGL không khả dụng. Chọn bản đồ online hoặc tải lại gói.'));
      instance.current = map;
      const observer = new ResizeObserver(() => map.resize()); observer.observe(container.current);
      return () => { observer.disconnect(); markers.current.forEach(marker => marker.remove()); markers.current = []; instance.current = null; map.remove(); };
    } catch {
      // The renderer failure is reported after initialization, without restarting map setup.
      let active = true;
      queueMicrotask(() => { if (active) setError('Trình duyệt không hỗ trợ WebGL để hiển thị bản đồ offline.'); });
      return () => { active = false; map?.remove(); };
    }
  }, [pack]);
  useEffect(() => {
    if (!instance.current) return;
    const map = instance.current;
    const render = () => {
    markers.current.forEach(marker => marker.remove()); markers.current = [];
    for (const group of clusterPlaces(places, p => map.project([p.longitude, p.latitude]), map.getZoom(), selectedId)) {
      if (group.places.length > 1) {
        const button = document.createElement('button'); button.type = 'button'; button.className = 'map-cluster'; button.textContent = String(group.places.length);
        button.setAttribute('aria-label', `${group.places.length} địa điểm, phóng to`);
        button.title = group.places.map(p => p.name).join(' · ');
        button.onclick = () => {
          map.fitBounds(group.places.reduce((bounds, place) => bounds.extend([place.longitude, place.latitude]), new maplibregl.LngLatBounds()), { padding: 40, maxZoom: 17, duration: 0 });
          render();
        };
        markers.current.push(new maplibregl.Marker({ element: button }).setLngLat([group.longitude, group.latitude]).addTo(map));
        continue;
      }
      const place = group.places[0];
      const button = document.createElement('button'); button.type = 'button'; button.className = 'offline-map-pin';
      button.textContent = '📍'; button.setAttribute('aria-label', place.name); button.title = place.name;
      const popup = document.createElement('div'); const title = document.createElement('strong'); title.textContent = place.name; popup.append(title);
      const address = document.createElement('p'); address.textContent = place.address || ''; popup.append(address);
      const detail = document.createElement('button'); detail.textContent = 'Xem chi tiết & nghe thuyết minh'; detail.onclick = () => callback.current(place.id); popup.append(detail);
      const marker = new maplibregl.Marker({ element: button }).setLngLat([place.longitude, place.latitude])
        .setPopup(new maplibregl.Popup({ offset: 24, focusAfterOpen: false }).setDOMContent(popup)).addTo(instance.current);
      button.addEventListener('click', () => callback.current(place.id)); markers.current.push(marker);
    }
    };
    render(); map.on('zoomend resize', render);
    return () => map.off('zoomend resize', render);
  }, [places, selectedId, pack]);
  return <><div ref={container} className="home-map" aria-label="Bản đồ vector Quận 1 offline" role="region" />
    <small>Gói Quận 1 · {pack.manifest.version} · {(pack.blob.size / 1024 / 1024).toFixed(1)} MB. Bản đồ giản lược đường, công trình và sông; không cần tải font hoặc sprite bên ngoài.</small>
    <p><a href={pack.manifest.sourceUrl} target="_blank" rel="noreferrer">Nguồn Protomaps / OpenStreetMap</a> · {pack.manifest.license}</p>
    {error && <p className="notice" role="alert">{error}</p>}</>;
}
