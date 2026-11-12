import { lazy, Suspense } from 'react';
import { BoTheoDoiTruyCap } from "./TheoDoiTruyCap";

const UngDung = lazy(() => import("./UngDung.jsx"));
const CongQuanTri = lazy(() => import("./CongQuanTri.jsx"));

export default function GocUngDung() {
  const portal = window.location.pathname.startsWith('/admin') || window.location.pathname.startsWith('/owner');
  return <BoTheoDoiTruyCap><Suspense fallback={<main role="status" style={{ padding: '32px', color: '#6b452c' }}>Đang mở SmartTour Guide…</main>}>
    {portal ? <CongQuanTri /> : <UngDung />}
  </Suspense></BoTheoDoiTruyCap>;
}
