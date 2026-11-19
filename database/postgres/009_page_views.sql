-- Adds a website-open event; existing POI views, playback and admin logs stay valid.
BEGIN;
SET LOCAL lock_timeout = '10s';
ALTER TABLE smarttour_demo.activity_logs DROP CONSTRAINT IF EXISTS activity_logs_kind_check;
ALTER TABLE smarttour_demo.activity_logs ADD CONSTRAINT activity_logs_kind_check
  CHECK (kind IN ('view','play','admin','page_view'));
COMMIT;
