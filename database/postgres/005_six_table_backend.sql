-- Six-table adapter prerequisites. No extra tables and no data deletion.
BEGIN;
DO $$ DECLARE item record; BEGIN
  IF (SELECT count(*) FROM information_schema.tables
      WHERE table_schema='smarttour_demo' AND table_type='BASE TABLE') <> 6 THEN
    RAISE EXCEPTION 'Expected exactly 6 tables in smarttour_demo; inspect/backup before migration.';
  END IF;
  IF to_regclass('smarttour_demo.roles') IS NOT NULL THEN
    RAISE EXCEPTION '15-table design detected; do not import with six-table adapter.';
  END IF;
  FOR item IN SELECT conrelid::regclass AS relation, conname FROM pg_constraint
    WHERE connamespace='smarttour_demo'::regnamespace AND contype='f' LOOP
    EXECUTE format('ALTER TABLE %s ALTER CONSTRAINT %I DEFERRABLE INITIALLY IMMEDIATE',item.relation,item.conname);
  END LOOP;
END $$;
COMMENT ON TABLE smarttour_demo.activity_logs IS
  'view/play/admin records. Reserved id system:platform-state:v1 contains typed auxiliary workflow collections, not an analytics event.';
COMMIT;
