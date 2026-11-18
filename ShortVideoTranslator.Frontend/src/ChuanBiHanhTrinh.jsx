import { useEffect, useRef } from 'react';
const copy = {
  vi: ['Chuẩn bị chuyến khám phá', 'Chọn ngôn ngữ thuyết minh', 'Danh sách địa điểm', 'Giao diện và thuyết minh ưu tiên', 'Đang chuẩn bị…', 'Sẵn sàng', 'Thử lại', 'Bắt đầu khám phá', 'Xem địa điểm trước', 'Trang yêu cầu quyền GPS để gợi ý địa điểm gần bạn. Bạn có thể từ chối hoặc dừng GPS. Vị trí làm tròn được chia sẻ với Admin tối đa 24 giờ. Thống kê mặc định bật, có thể tắt ở cuối trang.', 'Chưa sẵn sàng', 'Quay lại'],
  en: ['Prepare your visit', 'Narration language', 'Place catalog', 'Interface and priority narration', 'Preparing…', 'Ready', 'Retry', 'Start exploring', 'Browse places first', 'The page requests optional GPS access for nearby places. You can decline or stop GPS. Rounded locations are shared with Admin for up to 24 hours. Analytics is on by default and can be disabled in the footer.', 'Not ready', 'Back'],
  zh: ['准备游览', '选择讲解语言', '景点列表', '界面和优先讲解', '准备中…', '已就绪', '重试', '开始探索', '先浏览景点', '页面会请求可选的定位权限以推荐附近景点。您可以拒绝或停止定位。取整后的位置会分享给管理员，最长保留24小时。统计默认开启，可在页脚关闭。', '尚未就绪', '返回'],
  ja: ['観光の準備', '音声ガイドの言語', 'スポット一覧', '画面と優先音声ガイド', '準備中…', '準備完了', '再試行', '探索を始める', '先にスポットを見る', '近くのスポットのため位置情報の許可を求めます。拒否またはGPSを停止できます。丸めた位置は管理者に共有され、最大24時間保存されます。統計は初期設定でオンで、ページ下部で停止できます。', '未準備', '戻る'],
  ko: ['여행 준비', '해설 언어 선택', '장소 목록', '화면 및 우선 해설', '준비 중…', '준비 완료', '다시 시도', '탐색 시작', '장소 먼저 보기', '주변 장소를 위해 선택적인 위치 권한을 요청합니다. 거부하거나 GPS를 중지할 수 있습니다. 반올림한 위치는 관리자에게 공유되며 최대 24시간 보관됩니다. 통계는 기본으로 켜져 있으며 하단에서 끌 수 있습니다.', '준비되지 않음', '뒤로'],
  fr: ['Préparer votre visite', 'Langue du guide audio', 'Liste des lieux', 'Interface et commentaires prioritaires', 'Préparation…', 'Prêt', 'Réessayer', 'Commencer la visite', 'Voir les lieux d’abord', 'La page demande une autorisation GPS facultative pour les lieux proches. Vous pouvez refuser ou arrêter le GPS. La position arrondie est partagée avec l’administrateur pendant 24 heures au maximum. Les statistiques sont activées par défaut et désactivables en bas de page.', 'Indisponible', 'Retour'],
};
export default function ChuanBiHanhTrinh({ language, languages, network, onLanguage, loading, count, error, ready, preparationError, onRetry, onClose }) {
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
