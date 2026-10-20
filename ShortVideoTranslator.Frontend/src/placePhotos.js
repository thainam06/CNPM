export function placePhotos(place) {
  const cover = place.imageUrl ? [{ url: place.imageUrl, caption: 'Ảnh bìa', credit: place.imageNote, sourceUrl: place.imageSourceUrl, focusY: place.imageFocusY }] : [];
  const seen = new Set();
  return [...cover, ...(place.images || [])].filter(photo => photo.url && !seen.has(photo.url) && seen.add(photo.url));
}
