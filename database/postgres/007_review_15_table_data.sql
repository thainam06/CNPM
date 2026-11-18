-- READ ONLY. For the known 15-table schema created by 004 ONLY.
-- First inspect 006 / migration PLAN. Skip this file if already ALREADY_COMPACT.
-- Counts only: never export passwords, security-stamp values or state-envelope JSON.
-- Nonzero differences require human reconciliation before AcceptCompactSnapshot.
SELECT 'users.security_stamp' AS field, count(*) AS differing_nonnull_expanded_values
FROM smarttour_demo.users
WHERE security_stamp IS NOT NULL AND to_jsonb(security_stamp) IS DISTINCT FROM settings->'securityStamp'
UNION ALL
SELECT 'users.verified', count(*) FROM smarttour_demo.users
WHERE verified IS NOT NULL AND to_jsonb(verified) IS DISTINCT FROM settings->'verified'
UNION ALL
SELECT 'users.must_change_password', count(*) FROM smarttour_demo.users
WHERE must_change_password IS NOT NULL AND to_jsonb(must_change_password) IS DISTINCT FROM settings->'mustChangePassword'
UNION ALL
SELECT 'places.is_active', count(*) FROM smarttour_demo.places
WHERE is_active IS NOT NULL AND to_jsonb(is_active) IS DISTINCT FROM settings->'isActive'
UNION ALL
SELECT 'places.is_demo', count(*) FROM smarttour_demo.places
WHERE is_demo IS NOT NULL AND to_jsonb(is_demo) IS DISTINCT FROM settings->'isDemo'
UNION ALL
SELECT 'places.location_verified', count(*) FROM smarttour_demo.places
WHERE location_verified IS NOT NULL AND to_jsonb(location_verified) IS DISTINCT FROM settings->'locationVerified'
UNION ALL
SELECT 'places.description_vi', count(*) FROM smarttour_demo.places
WHERE description_vi IS NOT NULL AND to_jsonb(description_vi) IS DISTINCT FROM to_jsonb(introduction);

-- These workflows need explicit mapping if populated; migration preserves the
-- source and refuses to switch silently. Existing compact-envelope collections
-- are distinct from rows added to these expanded tables.
SELECT 'menu_items' AS table_name, count(*) AS row_count FROM smarttour_demo.menu_items
UNION ALL SELECT 'owner_requests', count(*) FROM smarttour_demo.owner_requests
UNION ALL SELECT 'place_proposals', count(*) FROM smarttour_demo.place_proposals
UNION ALL SELECT 'notifications', count(*) FROM smarttour_demo.notifications
UNION ALL SELECT 'audio_assets', count(*) FROM smarttour_demo.audio_assets
UNION ALL SELECT 'audio_tasks', count(*) FROM smarttour_demo.audio_tasks
UNION ALL SELECT 'audit_logs', count(*) FROM smarttour_demo.audit_logs
UNION ALL SELECT 'runtime_sessions', count(*) FROM smarttour_demo.runtime_sessions;
