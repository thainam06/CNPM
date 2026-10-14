import { useEffect, useRef, useState } from 'react';
import { createSpeech, getPlace, getPlaces, translate } from './api';
import './App.css';
import ExploreTools from './ExploreTools';
import { collectEvent } from './analytics';
import { readApi } from './api';
import HomeMap from './HomeMap';
import PlaceImage from './PlaceImage';
import { uiText } from './i18n';

const languages = [
  { code: 'vi', name: 'Tiếng Việt' }, { code: 'en', name: 'English' },
  { code: 'zh', name: '中文' }, { code: 'ja', name: '日本語' },
  { code: 'ko', name: '한국어' }, { code: 'fr', name: 'Français' },
];
const normalize = (value) => value.toLowerCase().normalize('NFD').replace(/[\u0300-\u036f]/g, '').replace(/đ/g, 'd');

function App() {
  const [places, setPlaces] = useState([]);
  const [selectedId, setSelectedId] = useState(null);
  const [place, setPlace] = useState(null);
  const [language, setLanguage] = useState(() => languages.some(item => item.code === localStorage.getItem('smarttour-language')) ? localStorage.getItem('smarttour-language') : 'vi');
  const [languageReady, setLanguageReady] = useState(false);
  const [languagePreparationError, setLanguagePreparationError] = useState('');
  const t = (key) => uiText(language, key);
  const [search, setSearch] = useState('');
  const [listLoading, setListLoading] = useState(true);
  const [detailLoading, setDetailLoading] = useState(false);
  const [listError, setListError] = useState('');
  const [detailError, setDetailError] = useState('');
  const [translation, setTranslation] = useState(null);
  const [translationLoading, setTranslationLoading] = useState(false);
  const [translationError, setTranslationError] = useState('');
  const [audioUrl, setAudioUrl] = useState(null);
  const [audioStatus, setAudioStatus] = useState('idle');
  const [audioError, setAudioError] = useState('');
  const [offlineMessage, setOfflineMessage] = useState('');
  const [menu, setMenu] = useState([]);
  const [autoPlace, setAutoPlace] = useState(null);
  const speakRef = useRef(null);
  const [reload, setReload] = useState(0);
  const [detailVersion, setDetailVersion] = useState(0);
  const [translationRetry, setTranslationRetry] = useState(0);
  const audioRef = useRef(null);
  const speechRequest = useRef(null);
  useEffect(() => {
    const listener = (event) => setOfflineMessage(event.detail);
    window.addEventListener('smarttour-offline', listener);
    return () => window.removeEventListener('smarttour-offline', listener);
  }, []);
  useEffect(() => {
    if (!place) return;
    const controller = new AbortController();
    readApi(`/places/${place.id}/menu`, controller.signal).then(data => { if (!controller.signal.aborted) setMenu(data); }).catch(() => {});
    return () => controller.abort();
  }, [place]);
  useEffect(() => { speakRef.current = speak; });
  useEffect(() => {
    if (autoPlace && place?.id === autoPlace && translation?.language === language && !translationLoading && languageReady && audioStatus === 'idle') {
      // Ref is synchronized before this effect, so it uses the selected place's source.
      speakRef.current?.();
    }
  }, [autoPlace, place, translation, translationLoading, language, languageReady, audioStatus]);

  useEffect(() => {
    const controller = new AbortController();
    getPlaces(controller.signal).then((data) => {
      if (controller.signal.aborted) return;
      setPlaces(data);
      setDetailLoading(data.length > 0);
      setDetailVersion((value) => value + 1);
      setSelectedId((previous) => data.some((item) => item.id === previous) ? previous : data[0]?.id ?? null);
    }).catch((error) => {
      if (!controller.signal.aborted) setListError(error.message);
    }).finally(() => { if (!controller.signal.aborted) setListLoading(false); });
    return () => controller.abort();
  }, [reload]);

  useEffect(() => {
    if (selectedId === null) return;
    const controller = new AbortController();
    getPlace(selectedId, controller.signal).then((data) => {
      if (!controller.signal.aborted) {
        setPlace(data);
        setTranslation(null);
        setTranslationLoading(true);
      }
    }).catch((error) => {
      if (!controller.signal.aborted) setDetailError(error.message);
    }).finally(() => { if (!controller.signal.aborted) setDetailLoading(false); });
    return () => controller.abort();
  }, [selectedId, detailVersion]);

  useEffect(() => {
    if (!place) return;
    const controller = new AbortController();
    translate(place.description, language, controller.signal).then((data) => {
      if (!controller.signal.aborted) setTranslation(data);
    }).catch((error) => {
      if (!controller.signal.aborted) setTranslationError(error.message);
    }).finally(() => { if (!controller.signal.aborted) setTranslationLoading(false); });
    return () => controller.abort();
  }, [place, language, translationRetry]);

  useEffect(() => () => { speechRequest.current?.abort(); }, []);
  useEffect(() => {
    if (!audioUrl) return;
    return () => URL.revokeObjectURL(audioUrl);
  }, [audioUrl]);
  useEffect(() => {
    if (!audioUrl || !audioRef.current) return;
    let cancelled = false;
    audioRef.current.play().catch(() => {
      if (!cancelled) {
        setAudioStatus('ready');
        setAudioError('Trình duyệt chưa cho phép tự phát. Nhấn nút phát trên trình phát MP3 bên dưới.');
      }
    });
    return () => { cancelled = true; };
  }, [audioUrl]);

  function stopAudio(clear = false) {
    speechRequest.current?.abort();
    speechRequest.current = null;
    if (audioRef.current) { audioRef.current.pause(); audioRef.current.currentTime = 0; }
    setAudioStatus(clear ? 'idle' : 'stopped');
    if (clear) { setAudioUrl(null); setAudioError(''); }
  }
  function choosePlace(id) {
    setAutoPlace(null);
    if (id === selectedId) return;
    stopAudio(true);
    setPlace(null);
    setMenu([]);
    setTranslation(null);
    setTranslationError('');
    setDetailError('');
    setDetailLoading(true);
    setSelectedId(id);
    collectEvent('view', id, language);
  }
  function chooseLanguage(value) {
    if (value === language) return;
    setLanguageReady(false); setLanguagePreparationError('');
    localStorage.setItem('smarttour-language', value);
    setAutoPlace(null);
    stopAudio(true);
    setTranslation(null);
    setTranslationError('');
    setTranslationLoading(true);
    setLanguage(value);
  }
  function retryPlaces() {
    stopAudio(true);
    setPlace(null);
    setTranslation(null);
    setListLoading(true);
    setListError('');
    setDetailError('');
    setTranslationError('');
    setReload((value) => value + 1);
  }
  function retryTranslation() {
    stopAudio(true);
    setTranslation(null);
    setTranslationLoading(true);
    setTranslationError('');
    setTranslationRetry((value) => value + 1);
  }
  async function speak() {
    setAutoPlace(null);
    if (audioStatus === 'loading' || audioStatus === 'playing') { stopAudio(); return; }
    setAudioError('');
    if (audioUrl && audioRef.current) {
      audioRef.current.currentTime = 0;
      try { await audioRef.current.play(); }
      catch { setAudioError('Không thể phát MP3. Hãy thử trình phát bên dưới.'); }
      return;
    }
    if (!place || !translation || !languageReady) return;
    const controller = new AbortController();
    speechRequest.current = controller;
    setAudioStatus('loading');
    try {
      // Send the original Vietnamese source; Backend translates before selecting the voice.
      const blob = await createSpeech(place.description, language, controller.signal);
      if (controller.signal.aborted) return;
      if (!blob.size || !blob.type.startsWith('audio/')) throw new Error('Backend không trả về MP3 hợp lệ.');
      setAudioUrl(URL.createObjectURL(blob));
      setAudioStatus('ready');
    } catch (error) {
      if (!controller.signal.aborted) { setAudioError(error.message); setAudioStatus('idle'); }
    } finally {
      if (speechRequest.current === controller) speechRequest.current = null;
    }
  }
  const filteredPlaces = places.filter((item) => normalize(`${item.name} ${item.category}`).includes(normalize(search.trim())));
  const statusText = t({ idle: 'idle', loading: 'loadingAudio', ready: 'ready', playing: 'playing', stopped: 'stopped' }[audioStatus]);

  return (
    <div className="app">
      <header className="header">
        <a className="brand" href="#gioi-thieu"><span className="brand-icon" aria-hidden="true">📍</span><div><h2>SmartTour Guide</h2><small>Khám phá Quận 1 cùng AI</small></div></a>
        <nav aria-label="Điều hướng"><a href="#gioi-thieu">{t('intro')}</a><a href="#ban-do">{t('map')}</a><a href="#dia-diem">{t('explore')}</a><a href="/owner">{t('owner')}</a><a href="/admin">{t('admin')}</a></nav>
        <div className="header-status">Bản demo • 6 ngôn ngữ</div>
      </header>
      <main className="container">
        <section className="hero-section" id="gioi-thieu">
          <span className="eyebrow">{t('eyebrow')}</span><h1>{t('heroTitle')}</h1>
          <p>{t('heroBody')}</p><a className="hero-link" href="#dia-diem">{t('start')} ↗</a>
          <label className="journey-language" htmlFor="journey-language">{t('tripLanguage')}<select id="journey-language" value={language} onChange={event => chooseLanguage(event.target.value)}>{languages.map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
        </section>
        <p className="notice">Nội dung thuyết minh là dữ liệu demo, cần kiểm chứng trước khi sử dụng chính thức. Ảnh có ghi nguồn; địa chỉ và tọa độ chỉ hiển thị khi có dữ liệu đã xác minh.</p>
        {offlineMessage && <p className="notice" role="status">{offlineMessage}</p>}
        <HomeMap language={language} places={places} selectedId={selectedId} onSelect={id => { choosePlace(id); document.getElementById('dia-diem')?.scrollIntoView({ behavior: 'smooth' }); }} />
        <ExploreTools places={places} language={language} onLanguageReadiness={status => { if (status.language === language) { setLanguageReady(status.ready); setLanguagePreparationError(status.error || ''); } }} onEnter={nearby => { choosePlace(nearby.id); setAutoPlace(nearby.id); }} />
        <section className="content-grid" id="dia-diem" aria-label="Khám phá địa điểm">
          <div className="places-panel">
            <div className="section-heading"><h2>{t('places')}</h2><span>{filteredPlaces.length} {t('count')}</span></div>
            <label className="search-label" htmlFor="search">{t('search')}</label><input id="search" className="search-input" type="search" placeholder={t('searchHint')} value={search} onChange={(event) => setSearch(event.target.value)} />
            {listLoading && <p role="status">{t('loadingPlaces')}</p>}
            {listError && <div className="notice" role="alert">{listError} <button onClick={retryPlaces}>Thử lại</button></div>}
            <div className="places-list">
              {filteredPlaces.map((item) => <button key={item.id} type="button" className={`place-card ${selectedId === item.id ? 'active' : ''}`} aria-pressed={selectedId === item.id} onClick={() => choosePlace(item.id)}>{item.imageUrl ? <PlaceImage key={item.imageUrl} place={item} compact /> : <span className="place-number" aria-hidden="true">0{item.id}</span>}<div className="place-info"><span className="category">{item.category}</span><h3>{item.name}</h3><p className="card-address">📍 {item.address || 'Địa chỉ chưa xác minh'}</p><span className="card-action">{item.isDemo ? 'Khám phá · demo' : 'Khám phá địa điểm'} →</span></div></button>)}
              {!listLoading && !listError && !filteredPlaces.length && <p role="status">{t('empty')}</p>}
            </div>
          </div>
          <article className="detail-panel" aria-label="Chi tiết địa điểm" aria-busy={detailLoading}>
            {detailLoading && <p className="detail-content" role="status">{t('loadingDetail')}</p>}
            {detailError && <div className="detail-content notice" role="alert">{detailError} <button onClick={retryPlaces}>Thử lại</button></div>}
            {!detailLoading && !detailError && place && <>
              {place.imageUrl ? <PlaceImage key={place.imageUrl} place={place} /> : <div className="detail-art" aria-hidden="true"><span>SMARTTOUR / QUẬN 1</span><strong>{place.name}</strong><small>Đồ họa trang trí • không phải ảnh địa điểm</small></div>}
              <div className="detail-content">
                <span className="category">{place.category}</span><h2>{place.name}</h2>{place.address && <div className="address-box"><span aria-hidden="true">📍</span><div><small>ĐỊA CHỈ</small><p>{place.address}</p>{place.addressSourceUrl && <a href={place.addressSourceUrl} target="_blank" rel="noreferrer">Nguồn địa chỉ ↗</a>}</div></div>}
                <h3>{t('description')} {place.isDemo && <small className="demo-badge">DEMO</small>}</h3><p className="description" lang="vi">{place.description}</p>
                {menu.length > 0 && <section className="public-menu"><h3>Thực đơn tham khảo</h3>{menu.map(item => <div key={item.id}><strong>{item.name}</strong><span>{item.price === null ? 'Giá chưa xác minh' : item.price.toLocaleString('vi-VN') + ' đ'}</span><p>{item.description}</p></div>)}<small>Giá do chủ quán/Admin cung cấp, cần xác nhận trước khi gọi món.</small></section>}
                {place.sourceUrl && <a href={place.sourceUrl} target="_blank" rel="noreferrer">Nguồn thông tin</a>}
                {place.locationVerified && Number.isFinite(place.latitude) && Number.isFinite(place.longitude) ? <><iframe className="place-map" title={`Bản đồ ${place.name}`} loading="lazy" referrerPolicy="no-referrer" src={`https://www.openstreetmap.org/export/embed.html?bbox=${place.longitude - .003}%2C${place.latitude - .003}%2C${place.longitude + .003}%2C${place.latitude + .003}&layer=mapnik&marker=${place.latitude}%2C${place.longitude}`} /><a href={`https://www.openstreetmap.org/?mlat=${place.latitude}&mlon=${place.longitude}#map=17/${place.latitude}/${place.longitude}`} target="_blank" rel="noreferrer">Mở bản đồ địa điểm ↗</a></> : <p className="map-note">Bản đồ chưa sẵn sàng: chưa có tọa độ đã xác minh.</p>}
                <section className="narration-panel" aria-label="Thuyết minh tự động">
                  <h3>🎧 {t('narration')}</h3><label htmlFor="language">{t('language')}</label>
                  <select id="language" value={language} onChange={(event) => chooseLanguage(event.target.value)}>{languages.map((item) => <option key={item.code} value={item.code}>{item.name}</option>)}</select>
                  {language !== 'vi' && <p className="provider-note">Dịch tự động bằng MyMemory, có thể sai tên riêng hoặc ngữ nghĩa. Dịch vụ miễn phí có hạn mức và cần Internet.</p>}
                  {translationLoading && <p role="status">{t('preparing')}</p>}
                  {translationError && <div className="notice" role="alert">{translationError} <button onClick={retryTranslation}>Thử dịch lại</button></div>}
                  {translation && <div className="translated-content" lang={language}><h4>{t(language === 'vi' ? 'original' : 'translated')}</h4><p>{translation.text}</p></div>}
                  {!languageReady && <p className="notice" role={languagePreparationError ? 'alert' : 'status'}>{languagePreparationError ? `Ngôn ngữ chưa sẵn sàng: ${languagePreparationError}. Nhấn “Chuẩn bị ngôn ngữ” để thử lại.` : 'Đang chuẩn bị giao diện và bộ thuyết minh ưu tiên…'}</p>}
                  <button className="listen-button" type="button" onClick={speak} disabled={!translation || translationLoading || !languageReady}>{audioStatus === 'loading' ? `■ ${t('cancel')}` : audioStatus === 'playing' ? `■ ${t('stop')}` : audioUrl ? `↻ ${t('replay')}` : `▶ ${t('listen')}`}</button>
                  <p className="audio-status" role="status" aria-live="polite">{statusText}</p>
                  {audioUrl && <audio ref={audioRef} src={audioUrl} controls preload="metadata" aria-label="Trình phát thuyết minh" onPlay={() => { setAudioStatus('playing'); collectEvent('play', place.id, language); }} onPause={() => setAudioStatus('stopped')} onEnded={() => setAudioStatus('ready')} onError={() => { setAudioError('Không thể giải mã hoặc phát MP3. Hãy tạo lại thuyết minh.'); setAudioUrl(null); setAudioStatus('idle'); }} />}
                  {audioError && <p className="notice" role="alert">{audioError}</p>}
                </section>
              </div>
            </>}
            {!place && !detailLoading && !detailError && <p className="detail-content">{t('pick')}</p>}
          </article>
        </section>
      </main>
      <footer className="footer">SmartTour Guide • Đồ án Công nghệ phần mềm<br /><a href="/admin">Quản trị viên</a> · <a href="/owner">Cổng chủ quán</a></footer>
    </div>
  );
}
export default App;
