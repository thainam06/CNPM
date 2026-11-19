-- SELECT only. Choose smarttour_demo in Table Editor / Schema Visualizer.
-- Online browsers: same 45-second window as PresenceService (one device, many tabs).
SELECT count(DISTINCT device_id) AS online_devices
FROM smarttour_demo.visitor_sessions
WHERE ended_at IS NULL AND last_seen_at > now() - interval '45 seconds';

SELECT device_id,
       device_info->'descriptor'->>'browser' AS browser,
       device_info->'descriptor'->>'operatingSystem' AS operating_system,
       device_info->'descriptor'->>'type' AS device_type,
       max(last_seen_at) AS last_seen_at
FROM smarttour_demo.visitor_sessions
WHERE ended_at IS NULL AND last_seen_at > now() - interval '45 seconds'
GROUP BY device_id, device_info->'descriptor'
ORDER BY last_seen_at DESC;

-- Do not display password_hash, private settings or the reserved platform state.
SELECT id, username, display_name, role, enabled FROM smarttour_demo.users;
SELECT id, name, address, latitude, longitude FROM smarttour_demo.places ORDER BY id;
SELECT id, kind, user_id, place_id, created_at
FROM smarttour_demo.activity_logs
WHERE id <> 'system:platform-state:v1'
ORDER BY created_at DESC LIMIT 30;
