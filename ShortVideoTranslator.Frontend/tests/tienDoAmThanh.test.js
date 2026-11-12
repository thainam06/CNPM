import test from 'node:test';
import assert from 'node:assert/strict';
import { theoDoiTienDoAmThanh } from '../src/theoDoiTienDoAmThanh.js';

test('SSE delivers progress; failure falls back once and cleanup blocks late updates', async () => {
  const luong={close(){this.closed=true;}}; const ketQua=[]; const lich=[]; let soLan=0;
  const dung=theoDoiTienDoAmThanh({taoLuong:()=>luong,taiDanhSach:async()=>{soLan++;return [{status:'completed'}];},
    khiCoDuLieu:d=>ketQua.push(d),khiLoi:()=>assert.fail(),henGio:fn=>{lich.push(fn);return 1;},huyHenGio:()=>{}});
  luong.onmessage({data:'[{"status":"running"}]'});
  assert.equal(ketQua[0][0].status,'running');
  luong.onerror(); luong.onerror(); await Promise.resolve(); await Promise.resolve();
  assert.equal(soLan,1); assert.equal(luong.closed,true); assert.equal(ketQua[1][0].status,'completed');
  dung(); luong.onmessage({data:'[]'}); assert.equal(ketQua.length,2);
});
test('Quick Tunnel uses abortable polling directly without opening SSE', async () => {
  let signal; let resolve; let delivered=false;
  const dung=theoDoiTienDoAmThanh({dungPolling:true,taoLuong:()=>assert.fail(),
    taiDanhSach:s=>{signal=s;return new Promise(r=>{resolve=r;});},khiCoDuLieu:()=>{delivered=true;},khiLoi:()=>assert.fail(),
    henGio:()=>assert.fail(),huyHenGio:()=>{}});
  dung(); resolve([]); await Promise.resolve(); assert.equal(signal.aborted,true); assert.equal(delivered,false);
});
