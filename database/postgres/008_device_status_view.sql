-- Read-only display for Supabase Table Editor. Six physical tables stay unchanged.
-- Status is computed at query time, never stored as a stale boolean.
BEGIN;
CREATE OR REPLACE VIEW smarttour_demo.thiet_bi_truy_cap
WITH (security_invoker = true) AS
WITH devices AS (
  SELECT device_id,
         (array_agg(device_info ORDER BY last_seen_at DESC))[1] AS info,
         max(last_seen_at) AS last_seen,
         count(*) FILTER (WHERE ended_at IS NULL AND last_seen_at > now() - interval '45 seconds') AS active_tabs
  FROM smarttour_demo.visitor_sessions
  GROUP BY device_id
)
SELECT device_id AS ma_thiet_bi,
       coalesce(info->'descriptor'->>'browser', 'Other') AS trinh_duyet,
       coalesce(info->'descriptor'->>'operatingSystem', 'Other') AS he_dieu_hanh,
       coalesce(info->'descriptor'->>'type', 'Unknown') AS loai_thiet_bi,
       CASE WHEN active_tabs > 0 THEN 'Đang hoạt động' ELSE 'Đã ngắt kết nối' END AS trang_thai,
       active_tabs AS so_tab_hoat_dong,
       last_seen AS lan_cuoi_hoat_dong
FROM devices;
COMMENT ON VIEW smarttour_demo.thiet_bi_truy_cap IS
  'Device status: active if any tab has a heartbeat within 45 seconds. Refresh to recalculate. Closed/expired sessions may be deleted by the application.';
REVOKE ALL ON smarttour_demo.thiet_bi_truy_cap FROM PUBLIC;
-- Supabase browser roles must not access device data through this display view.
DO $$ DECLARE target_role text; BEGIN
  FOREACH target_role IN ARRAY ARRAY['anon','authenticated'] LOOP
    IF EXISTS (SELECT 1 FROM pg_roles WHERE rolname=target_role) THEN
      EXECUTE format('REVOKE ALL ON smarttour_demo.thiet_bi_truy_cap FROM %I', target_role);
    END IF;
  END LOOP;
END $$;
COMMIT;
