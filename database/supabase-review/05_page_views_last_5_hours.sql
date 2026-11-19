-- Website opens/reloads only; POI views and heartbeats are excluded.
SELECT count(*) AS luot_mo_website_5_gio
FROM smarttour_demo.activity_logs
WHERE kind = 'page_view'
  AND created_at >= now() - interval '5 hours'
  AND created_at <= now();

SELECT created_at AT TIME ZONE 'Asia/Ho_Chi_Minh' AS thoi_gian,
       device_id AS ma_trinh_duyet
FROM smarttour_demo.activity_logs
WHERE kind = 'page_view'
ORDER BY created_at DESC LIMIT 30;
