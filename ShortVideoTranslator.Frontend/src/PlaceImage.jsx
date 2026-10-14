import { useState } from 'react';
import { baseUrl } from './api';

export default function PlaceImage({ place, compact = false }) {
  const [failed, setFailed] = useState(false);
  const src = place.imageUrl?.startsWith('/api/') ? baseUrl + place.imageUrl : place.imageUrl;
  const image = failed || !src ? <div className={compact ? 'thumbnail-fallback' : 'photo-fallback'}><span aria-hidden="true">📍</span>{!compact && <p>Ảnh chưa tải được. Bạn vẫn có thể đọc và nghe thuyết minh.</p>}</div> :
    <img className={compact ? 'place-thumbnail' : 'detail-image'} style={{ objectPosition: `center ${place.imageFocusY ?? 25}%` }} src={src} alt={place.name} loading={compact ? 'lazy' : 'eager'} decoding="async" referrerPolicy="no-referrer" onError={() => setFailed(true)} />;
  if (compact) return image;
  return <figure className="place-photo"><div className="photo-stage">{image}<div className="photo-title"><small>{place.category}</small><strong>{place.name}</strong></div></div><figcaption>{place.imageNote}{place.imageSourceUrl && <> · <a href={place.imageSourceUrl} target="_blank" rel="noreferrer">Nguồn ảnh</a></>}</figcaption></figure>;
}
