// Ưu tiên SSE; Quick Tunnel dùng polling vì không hỗ trợ SSE.
export function theoDoiTienDoAmThanh({ taoLuong, taiDanhSach, khiCoDuLieu, khiLoi,
  dungPolling = false, henGio = setTimeout, huyHenGio = clearTimeout }) {
  let daDung = false; let luong; let lich;
  const boDieuKhien = new AbortController();
  async function taiLai() {
    try {
      const duLieu = await taiDanhSach(boDieuKhien.signal);
      if (!daDung) khiCoDuLieu(duLieu);
    } catch (loi) { if (!daDung) khiLoi(loi); }
    finally { if (!daDung) lich = henGio(taiLai, 5000); }
  }
  function chuyenSangPolling() {
    if (daDung || dungPolling) return;
    dungPolling = true; luong?.close(); taiLai();
  }
  if (dungPolling || !taoLuong) { dungPolling = true; taiLai(); }
  else {
    try {
      luong = taoLuong();
      luong.onmessage = suKien => {
        if (daDung) return;
        try {
          const duLieu = JSON.parse(suKien.data);
          if (!Array.isArray(duLieu)) throw new Error('Dữ liệu tiến độ audio không hợp lệ.');
          khiCoDuLieu(duLieu);
        } catch { chuyenSangPolling(); }
      };
      luong.onerror = chuyenSangPolling;
    } catch { chuyenSangPolling(); }
  }
  return () => { daDung = true; boDieuKhien.abort(); huyHenGio(lich); luong?.close(); };
}
