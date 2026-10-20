import { useEffect, useMemo, useRef, useState } from 'react';
import PlaceImage from './PlaceImage';
import { placePhotos } from './placePhotos';

const picture = (place, photo) => ({ ...place, imageUrl: photo.url, imageNote: [photo.caption, photo.credit].filter(Boolean).join(' — '), imageSourceUrl: photo.sourceUrl, imageFocusY: photo.focusY ?? 50, imageAlt: `${place.name} — ${photo.caption}` });
export default function PlaceGallery({ place }) {
  const [selected, setSelected] = useState('');
  const [autoplay, setAutoplay] = useState(() => !window.matchMedia('(prefers-reduced-motion: reduce)').matches);
  const [hovered, setHovered] = useState(false);
  const [focused, setFocused] = useState(false);
  const [visible, setVisible] = useState(false);
  const [pageVisible, setPageVisible] = useState(!document.hidden);
  const gallery = useRef(null);
  const photos = useMemo(() => placePhotos(place), [place]);
  const current = photos.find(photo => photo.url === selected) || photos[0];
  const index = photos.indexOf(current);
  useEffect(() => {
    const observer = new IntersectionObserver(entries => setVisible(entries[0].isIntersecting), { threshold: 0.2 });
    if (gallery.current) observer.observe(gallery.current);
    const visibility = () => setPageVisible(!document.hidden);
    document.addEventListener('visibilitychange', visibility);
    return () => { observer.disconnect(); document.removeEventListener('visibilitychange', visibility); };
  }, []);
  useEffect(() => {
    if (!autoplay || hovered || focused || !visible || !pageVisible || photos.length < 2) return;
    const timer = window.setInterval(() => setSelected(previous => photos[(Math.max(0, photos.findIndex(photo => photo.url === previous)) + 1) % photos.length].url), 3000);
    return () => window.clearInterval(timer);
  }, [autoplay, hovered, focused, visible, pageVisible, photos, selected]);
  function step(direction) { setSelected(photos[(index + direction + photos.length) % photos.length].url); }
  if (!current) return null;
  return <section ref={gallery} className="place-gallery" aria-label={`Ảnh ${place.name}`} onMouseEnter={() => setHovered(true)} onMouseLeave={() => setHovered(false)} onFocus={() => setFocused(true)} onBlur={event => { if (!event.currentTarget.contains(event.relatedTarget)) setFocused(false); }}>
    <PlaceImage key={current.url} place={picture(place, current)} />
    {photos.length > 1 && <div className="gallery-controls"><span>Ảnh {index + 1}/{photos.length} · 3 giây/ảnh</span><div>
      <button type="button" aria-label="Ảnh trước" onClick={() => step(-1)}>←</button>
      <button type="button" aria-pressed={autoplay} onClick={() => setAutoplay(value => !value)}>{autoplay ? 'Tạm dừng tự chuyển' : 'Bật tự chuyển ảnh'}</button>
      <button type="button" aria-label="Ảnh tiếp theo" onClick={() => step(1)}>→</button>
    </div></div>}
    {photos.length > 1 && <div className="gallery-thumbnails" role="group" aria-label="Chọn góc nhìn địa điểm">
      {photos.map((photo, index) => <button key={photo.url} type="button" aria-pressed={photo.url === current.url} aria-label={`Xem ảnh ${index + 1}: ${photo.caption}`} onClick={() => setSelected(photo.url)}>
        <PlaceImage key={photo.url} place={picture(place, photo)} compact /><span>{photo.caption}</span>
      </button>)}
    </div>}
  </section>;
}
