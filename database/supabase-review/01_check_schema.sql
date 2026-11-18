-- READ ONLY. Run first in Supabase SQL Editor with permission to inspect smarttour_demo.
-- Repository names: users and places, not user and port. Do not create aliases/tables.

-- A. Six required tables and any unexpected extra tables.
WITH expected(table_name) AS (
  VALUES ('users'), ('places'), ('place_images'), ('place_narrations'),
         ('visitor_sessions'), ('activity_logs')
), actual AS (
  SELECT table_name FROM information_schema.tables
  WHERE table_schema = 'smarttour_demo' AND table_type = 'BASE TABLE'
)
SELECT 'smarttour_demo' AS schema_name,
       coalesce(e.table_name, a.table_name) AS table_name,
       CASE WHEN a.table_name IS NULL THEN 'MISSING_OR_NOT_VISIBLE'
            WHEN e.table_name IS NULL THEN 'UNEXPECTED_TABLE'
            ELSE 'FOUND' END AS result
FROM expected e FULL JOIN actual a USING (table_name)
ORDER BY table_name;

-- B. Columns, types, nullability. No credential values or record payloads.
SELECT table_schema, table_name, ordinal_position, column_name,
       data_type, udt_name, is_nullable
FROM information_schema.columns
WHERE table_schema = 'smarttour_demo'
ORDER BY table_name, ordinal_position;

-- C. PK, FK, unique and check constraints, including migration 005 prerequisites.
SELECT t.relname AS table_name, c.conname AS constraint_name,
       c.contype AS constraint_type, pg_get_constraintdef(c.oid) AS definition,
       c.convalidated AS validated, c.condeferrable AS deferrable,
       c.condeferred AS initially_deferred
FROM pg_constraint c
JOIN pg_class t ON t.oid = c.conrelid
JOIN pg_namespace n ON n.oid = t.relnamespace
WHERE n.nspname = 'smarttour_demo'
ORDER BY t.relname, c.conname;

-- D. RLS and indexes. Enabled RLS alone does not prove the correct permissions.
SELECT schemaname, tablename, rowsecurity AS rls_enabled
FROM pg_tables WHERE schemaname = 'smarttour_demo' ORDER BY tablename;

SELECT schemaname, tablename, indexname, indexdef
FROM pg_indexes WHERE schemaname = 'smarttour_demo'
ORDER BY tablename, indexname;
