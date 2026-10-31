-- READ ONLY. Run after schema inspection. Violations should all be zero.
-- Checks are limited to rows visible to the executing database role.
SELECT 'image_without_place' AS check_name, count(*) AS violations
FROM smarttour_demo.place_images i
WHERE NOT EXISTS (SELECT 1 FROM smarttour_demo.places p WHERE p.id = i.place_id)
UNION ALL
SELECT 'narration_without_place', count(*) FROM smarttour_demo.place_narrations n
WHERE NOT EXISTS (SELECT 1 FROM smarttour_demo.places p WHERE p.id = n.place_id)
UNION ALL
SELECT 'place_without_owner_record', count(*) FROM smarttour_demo.places p
WHERE p.owner_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM smarttour_demo.users u WHERE u.id = p.owner_id)
UNION ALL
SELECT 'log_without_place_record', count(*) FROM smarttour_demo.activity_logs a
WHERE a.place_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM smarttour_demo.places p WHERE p.id = a.place_id)
UNION ALL
SELECT 'log_without_user_record', count(*) FROM smarttour_demo.activity_logs a
WHERE a.user_id IS NOT NULL
  AND NOT EXISTS (SELECT 1 FROM smarttour_demo.users u WHERE u.id = a.user_id)
UNION ALL
SELECT 'view_play_without_consent_or_device', count(*) FROM smarttour_demo.activity_logs
WHERE kind IN ('view', 'play') AND (NOT consent OR nullif(btrim(device_id), '') IS NULL)
UNION ALL
SELECT 'invalid_narration_language', count(*) FROM smarttour_demo.place_narrations
WHERE language_code NOT IN ('vi', 'en', 'zh', 'ja', 'ko', 'fr')
UNION ALL
SELECT 'duplicate_approved_language_groups', count(*) FROM (
  SELECT place_id, language_code FROM smarttour_demo.place_narrations
  WHERE status = 'approved' GROUP BY place_id, language_code HAVING count(*) > 1
) duplicate_narrations
UNION ALL
SELECT 'duplicate_cover_groups', count(*) FROM (
  SELECT place_id FROM smarttour_demo.place_images
  WHERE is_cover GROUP BY place_id HAVING count(*) > 1
) duplicate_covers
UNION ALL
SELECT 'empty_narration_text', count(*) FROM smarttour_demo.place_narrations
WHERE nullif(btrim(narration_text), '') IS NULL
UNION ALL
SELECT 'invalid_place_coordinates', count(*) FROM smarttour_demo.places
WHERE (latitude IS NULL) <> (longitude IS NULL)
   OR latitude NOT BETWEEN -90 AND 90 OR longitude NOT BETWEEN -180 AND 180
UNION ALL
SELECT 'invalid_session_timestamps', count(*) FROM smarttour_demo.visitor_sessions
WHERE last_seen_at < started_at OR ended_at < started_at;

-- Adapter 005 requires all FKs to be deferrable. Inspect actual constraints in 01 too.
SELECT count(*) AS foreign_keys,
       count(*) FILTER (WHERE NOT c.condeferrable) AS non_deferrable_foreign_keys
FROM pg_constraint c JOIN pg_namespace n ON n.oid = c.connamespace
WHERE n.nspname = 'smarttour_demo' AND c.contype = 'f';

-- Informational: only applies to a DB imported/used by the six-table backend adapter.
-- Do NOT print details: they can contain security/refresh state and private workflows.
SELECT count(*) AS backend_state_envelopes
FROM smarttour_demo.activity_logs WHERE id = 'system:platform-state:v1';

-- Content warnings, not automatically database corruption.
SELECT p.id, p.name,
       NOT EXISTS (SELECT 1 FROM smarttour_demo.place_images i WHERE i.place_id = p.id)
         AS no_gallery_image,
       NOT EXISTS (
         SELECT 1 FROM smarttour_demo.place_narrations n
         WHERE n.place_id = p.id AND n.language_code = 'vi' AND n.status = 'approved'
       ) AS no_approved_vietnamese_source
FROM smarttour_demo.places p ORDER BY p.id;
-- A cover may be retained in places.settings by the codec, outside place_images.
-- No gallery image alone is therefore not proof the UI has no cover image.
