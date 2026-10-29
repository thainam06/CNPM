-- Convert the earlier six-table design in place, preserving removed values in JSONB.
-- Applies only to smarttour_demo. Does not touch smarttour or the running SQLite API.
BEGIN;
SET LOCAL lock_timeout = '10s';

DO $$
DECLARE
  spec record;
  col record;
  keep_columns text[];
BEGIN
  IF to_regclass('smarttour_demo.roles') IS NOT NULL THEN
    RAISE EXCEPTION 'PRD-aligned schema detected. Do not run the obsolete compact conversion.';
  END IF;
  -- Check all tables before changing any of them.
  FOR spec IN SELECT * FROM (VALUES
    ('users'), ('places'), ('place_images'), ('place_narrations'),
    ('visitor_sessions'), ('activity_logs')
  ) AS expected(table_name) LOOP
    IF to_regclass(format('smarttour_demo.%I', spec.table_name)) IS NULL THEN
      RAISE EXCEPTION 'Missing smarttour_demo.%: run the appropriate create script first', spec.table_name;
    END IF;
  END LOOP;

  FOR spec IN SELECT * FROM (VALUES
    ('users', 'settings', ARRAY['id','username','display_name','password_hash','role','enabled']),
    ('places', 'settings', ARRAY['id','owner_id','name','address','latitude','longitude','introduction']),
    ('place_images', 'metadata', ARRAY['id','place_id','url','caption','ordinal','is_cover']),
    ('place_narrations', 'metadata', ARRAY['id','place_id','language_code','narration_text','audio_path','status','version']),
    ('visitor_sessions', 'device_info', ARRAY['id','device_id','started_at','last_seen_at','ended_at']),
    ('activity_logs', 'details', ARRAY['id','kind','user_id','device_id','place_id','consent','created_at'])
  ) AS config(table_name, extra_column, main_columns) LOOP
    EXECUTE format('LOCK TABLE smarttour_demo.%I IN ACCESS EXCLUSIVE MODE', spec.table_name);
    EXECUTE format(
      'ALTER TABLE smarttour_demo.%I ADD COLUMN IF NOT EXISTS %I jsonb NOT NULL DEFAULT ''{}''',
      spec.table_name, spec.extra_column);

    keep_columns := spec.main_columns || ARRAY[spec.extra_column];
    -- Preserve all removed column values, including nulls. Current SQL columns
    -- take precedence over same-named metadata keys.
    EXECUTE format(
      'UPDATE smarttour_demo.%I AS t SET %I = COALESCE(t.%I, ''{}''::jsonb) || (to_jsonb(t) - $1)',
      spec.table_name, spec.extra_column, spec.extra_column)
      USING keep_columns;

    FOR col IN SELECT column_name FROM information_schema.columns
      WHERE table_schema='smarttour_demo' AND table_name=spec.table_name
        AND NOT (column_name = ANY(keep_columns)) LOOP
      -- No CASCADE: unknown external dependencies cause rollback, not deletion.
      EXECUTE format('ALTER TABLE smarttour_demo.%I DROP COLUMN %I', spec.table_name, col.column_name);
    END LOOP;
  END LOOP;
END $$;
COMMIT;

-- Expect 6 rows with column counts: 8, 7, 8, 8, 7, 6 respectively by table name.
SELECT table_name, count(*) AS column_count
FROM information_schema.columns
WHERE table_schema='smarttour_demo'
GROUP BY table_name ORDER BY table_name;
