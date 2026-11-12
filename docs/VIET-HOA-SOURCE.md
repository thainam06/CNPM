# Bản đồ tên source Frontend sau Việt hóa

Ngày cập nhật: 12/11/2026. Đổi tên module và các hàm nghiệp vụ Frontend; không đổi namespace C#, URL API, tên trường JSON, khóa IndexedDB hoặc tên bảng database. Backend chưa được đổi toàn bộ tên hàm.

## File / module

| Tên cũ | Tên mới |
|---|---|
| App | UngDung |
| Root | GocUngDung |
| Portal | CongQuanTri |
| AiAdvisorPanel | TroLyBienTap |
| DistanceCalculator | DoKhoangCach |
| DeviceMonitor | ThongKeThietBi |
| ExploreTools | TienIchKhamPha |
| HomeMap | BanDoTrangChu |
| NearbyPlaces | DiaDiemGanBan |
| OfflineBundleTools | CongCuGoiNgoaiTuyen |
| OfflineMap | BanDoNgoaiTuyen |
| OwnerNotifications | ThongBaoChuQuan |
| PlaceGallery | BoAnhDiaDiem |
| PlaceImage | AnhDiaDiem |
| PoiManager | QuanLyDiaDiem |
| PortalLocale | NgonNguQuanTri |
| Presence | TheoDoiTruyCap |
| PresenceMonitor | ThietBiTrucTuyen |
| RegistrationStatus | TrangThaiDangKy |
| StartupPanel | ChuanBiHanhTrinh |
| TranslationPanel | QuanLyBanDich |
| api | dichVuApi |
| analytics | thongKe |
| analyticsQueue | hangDoiThongKe |
| bundleService | dichVuGoiNgoaiTuyen |
| cachePolicy | chinhSachBoNho |
| deviceDescriptor | moTaThietBi |
| deviceId | maThietBi |
| geofence | vungKichHoat |
| i18n | ngonNguGiaoDien |
| localization | chuanBiNgonNgu |
| locationTracking | theoDoiViTri |
| locationWatch | giamSatDinhVi |
| mapClusters | gomCumBanDo |
| mapPack | goiBanDo |
| narrationKey | khoaThuyetMinh |
| nearbyPlaces | xepHangGanDay |
| notificationRoute | duongDanThongBao |
| offline | khoNgoaiTuyen |
| placePhotos | duLieuAnhDiaDiem |
| poiCatalog | danhMucDiaDiem |
| portalMessages | tuDienQuanTri |
| presenceIdentity | dinhDanhTruyCap |
| presenceTracker | vongDoiTruyCap |
| readyNarration | thuyetMinhSanSang |
| runtimePolicy | chinhSachChiaSeViTri |
| startupProbe | kiemTraKhoiDong |
| triggerRadius | banKinhKichHoat |
| useNearbyLocation | useDinhViGanDay |

## Hàm nghiệp vụ tiêu biểu

| Hàm cũ | Hàm mới |
|---|---|
| distanceMeters | tinhKhoangCachMet |
| getPlaces / getPlace | layDanhSachDiaDiem / layChiTietDiaDiem |
| createSpeech / translate | taoThuyetMinh / dichNoiDung |
| startPresenceTracking | batDauTheoDoiTruyCap |
| normalizeTriggerRadius | chuanHoaBanKinhKichHoat |
| readApi / mutateApi | docApi / ghiApi |
| cacheLocalization | luuNgonNguDiaDiem |
| useNearbyLocation | useDinhViGanDay |

Tên React hooks vẫn có tiền tố use. Các tên bắt buộc của thư viện, các trường DTO và cột database giữ nguyên để tương thích. Import, nơi gọi hàm và kiểm thử được cập nhật cùng lúc.

Tên file .js và .jsx không dùng cùng basename khác chữ hoa/thường, tránh lỗi phân giải module trên Windows/Linux.
