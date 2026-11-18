-- READ ONLY. Run after 01 confirms the six tables and their columns.
-- Counts are current query results, not the historical counts in documentation.

SELECT 'users' AS table_name, count(*) AS row_count FROM smarttour_demo.users
UNION ALL SELECT 'places', count(*) FROM smarttour_demo.places
UNION ALL SELECT 'place_images', count(*) FROM smarttour_demo.place_images
UNION ALL SELECT 'place_narrations', count(*) FROM smarttour_demo.place_narrations
UNION ALL SELECT 'visitor_sessions', count(*) FROM smarttour_demo.visitor_sessions
UNION ALL SELECT 'activity_logs', count(*) FROM smarttour_demo.activity_logs;

-- Accounts: deliberately exclude password_hash, username and settings/security stamps.
SELECT id, role, enabled FROM smarttour_demo.users ORDER BY id LIMIT 50;

SELECT id, name, address, latitude, longitude,
       left(introduction, 160) AS introduction_preview
FROM smarttour_demo.places ORDER BY id LIMIT 50;

-- Avoid dumping URLs/metadata that could include signed media links or private fields.
SELECT p.id AS place_id, p.name, i.id AS image_id, i.ordinal, i.is_cover,
       nullif(btrim(i.url), '') IS NOT NULL AS has_image_url
FROM smarttour_demo.places p
LEFT JOIN smarttour_demo.place_images i ON i.place_id = p.id
ORDER BY p.id, i.ordinal LIMIT 100;

-- has_audio_path is metadata only: it does NOT prove an MP3 exists or plays correctly.
SELECT p.id AS place_id, p.name, n.language_code, n.status, n.version,
       length(n.narration_text) AS text_characters,
       nullif(btrim(n.audio_path), '') IS NOT NULL AS has_audio_path
FROM smarttour_demo.places p
LEFT JOIN smarttour_demo.place_narrations n ON n.place_id = p.id
ORDER BY p.id, n.language_code, n.version LIMIT 100;

-- Real codec shape: device_info.descriptor.{browser,operatingSystem,type}.
-- Group labels rather than publish individual session/device identifiers.
SELECT coalesce(device_info #>> '{descriptor,browser}', 'Unknown') AS browser,
       coalesce(device_info #>> '{descriptor,operatingSystem}', 'Unknown') AS os,
       coalesce(device_info #>> '{descriptor,type}', 'Unknown') AS device_type,
       count(DISTINCT device_id) AS online_browsers
FROM smarttour_demo.visitor_sessions
WHERE ended_at IS NULL AND last_seen_at > now() - interval '45 seconds'
GROUP BY 1, 2, 3 ORDER BY 1, 2, 3;

-- The reserved state envelope contains auxiliary workflows: do NOT export its details.
SELECT kind, count(*) AS row_count, min(created_at) AS first_event,
       max(created_at) AS latest_event
FROM smarttour_demo.activity_logs
WHERE id <> 'system:platform-state:v1'
GROUP BY kind ORDER BY kind;

SELECT place_id,
       count(*) FILTER (WHERE kind = 'view' AND consent) AS views,
       count(*) FILTER (WHERE kind = 'play' AND consent) AS plays
FROM smarttour_demo.activity_logs
WHERE id <> 'system:platform-state:v1' AND kind IN ('view', 'play')
GROUP BY place_id ORDER BY place_id NULLS LAST;
