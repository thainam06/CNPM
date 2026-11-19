-- Prepare the six-table backend when the earlier 15-table demo is EMPTY.
-- Never drops tables or records. The entire legacy schema is retained.
-- Execute this file, then 002_smarttour_compact.sql and 005_six_table_backend.sql.
-- Stop the application before preparing/importing; do not run against a live cloud backend.
BEGIN;
SET LOCAL lock_timeout = '10s';
DO $$
DECLARE item record; row_exists boolean;
BEGIN
  IF to_regnamespace('smarttour_demo') IS NULL THEN RETURN; END IF;
  IF to_regclass('smarttour_demo.roles') IS NULL THEN
    RAISE EXCEPTION 'Not the legacy 15-table design; inspect the schema instead of converting it.';
  END IF;
  IF to_regnamespace('smarttour_demo_legacy_15') IS NOT NULL THEN
    RAISE EXCEPTION 'Legacy archive already exists; inspect before continuing.';
  END IF;
  IF (SELECT count(*) FROM pg_tables WHERE schemaname='smarttour_demo') <> 15 THEN
    RAISE EXCEPTION 'Expected the known 15-table design; no changes made.';
  END IF;
  -- Lock all tables first, then recheck emptiness to prevent concurrent inserts.
  FOR item IN SELECT tablename FROM pg_tables WHERE schemaname='smarttour_demo' ORDER BY tablename LOOP
    EXECUTE format('LOCK TABLE smarttour_demo.%I IN ACCESS EXCLUSIVE MODE', item.tablename);
  END LOOP;
  FOR item IN SELECT tablename FROM pg_tables WHERE schemaname='smarttour_demo' ORDER BY tablename LOOP
    EXECUTE format('SELECT EXISTS (SELECT 1 FROM smarttour_demo.%I)', item.tablename) INTO row_exists;
    IF row_exists THEN
      RAISE EXCEPTION 'Legacy schema contains data. Back up and migrate explicitly; no changes made.';
    END IF;
  END LOOP;
  ALTER SCHEMA smarttour_demo RENAME TO smarttour_demo_legacy_15;
END $$;
COMMIT;
