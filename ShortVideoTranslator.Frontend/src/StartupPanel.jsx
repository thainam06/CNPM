import { useEffect, useRef } from 'react';
const copy = {
  vi: ['Chuẩn bị chuyến khám phá', 'Chọn ngôn ngữ thuyết minh', 'Danh sách địa điểm', 'Giao diện và thuyết minh ưu tiên', 'Đang chuẩn bị…', 'Sẵn sàng', 'Thử lại', 'Bắt đầu khám phá', 'Xem địa điểm trước', 'Định vị là tùy chọn. Bạn có thể bật trong Tiện ích; mặc định không gửi vị trí hay thống kê.', 'Chưa sẵn sàng', 'Quay lại'],
  en: ['Prepare your visit', 'Narration language', 'Place catalog', 'Interface and priority narration', 'Preparing…', 'Ready', 'Retry', 'Start exploring', 'Browse places first', 'Location is optional. Enable it in Explore tools; location sharing and analytics are off by default.', 'Not ready', 'Back'],
  zh: ['准备游览', '选择讲解语言', '景点列表', '界面和优先讲解', '准备中…', '已就绪', '重试', '开始探索', '先浏览景点', '定位为可选功能。可在探索工具中开启；位置共享和统计默认关闭。', '尚未就绪', '返回'],
  ja: ['観光の準備', '音声ガイドの言語', 'スポット一覧', '画面と優先音声ガイド', '準備中…', '準備完了', '再試行', '探索を始める', '先にスポットを見る', '位置情報は任意です。探索ツールから有効にできます。位置共有と統計は初期設定でオフです。', '未準備', '戻る'],
  ko: ['여행 준비', '해설 언어 선택', '장소 목록', '화면 및 우선 해설', '준비 중…', '준비 완료', '다시 시도', '탐색 시작', '장소 먼저 보기', '위치 기능은 선택 사항입니다. 탐색 도구에서 켤 수 있으며 위치 공유 및 통계는 기본적으로 꺼져 있습니다.', '준비되지 않음', '뒤로'],
  fr: ['Préparer votre visite', 'Langue du guide audio', 'Liste des lieux', 'Interface et commentaires prioritaires', 'Préparation…', 'Prêt', 'Réessayer', 'Commencer la visite', 'Voir les lieux d’abord', 'La localisation est facultative. Activez-la dans les outils ; partage de position et statistiques désactivés par défaut.', 'Indisponible', 'Retour'],
};
export default function StartupPanel({ language, languages, network, onLanguage, loading, count, error, ready, preparationError, onRetry, onClose }) {
  const dialog = useRef(null); const c = copy[language] || copy.vi;
  useEffect(() => {
    const node = dialog.current; const previous = document.activeElement;
    node.showModal();
    return () => { node.close(); previous?.focus?.(); };
  }, []);
  return <dialog ref={dialog} className="startup-dialog" onCancel={event => { event.preventDefault(); onClose(); }} aria-labelledby="startup-title">
    <div className="startup-art" aria-hidden="true">📍<span>SMARTTOUR GUIDE · QUẬN 1</span></div>
    <div className="startup-copy"><span className="eyebrow">SÀI GÒN · VIỆT NAM</span><h1 id="startup-title">{c[0]}</h1>
      <label>{c[1]}<select value={language} onChange={event => onLanguage(event.target.value)}>{languages.map(item => <option key={item.code} value={item.code}>{item.name}</option>)}</select></label>
      <ul className="startup-checks" aria-live="polite"><li><span>{c[2]}</span><strong>{loading ? c[4] : error ? c[10] : `${count} ✓`}</strong></li><li><span>{c[3]}</span><strong>{ready ? c[5] + ' ✓' : preparationError ? c[10] : c[4]}</strong></li></ul>
      {(error || preparationError) && <p className="notice" role="alert">{error || preparationError}</p>}
      <p role="status">Backend · {network === 'ready' ? c[5] + ' ✓' : network === 'checking' ? c[4] : c[10]}{network === 'unavailable' && ' · Offline cache'}</p>
      <p className="muted">{c[9]}</p><div className="actions"><button className="listen-button" disabled={loading || !ready || !!error} onClick={onClose}>{c[7]}</button><button onClick={onClose}>{c[8]}</button>{(error || preparationError || network === 'unavailable') && <button onClick={onRetry}>{c[6]}</button>}</div>
    </div>
  </dialog>;
}
