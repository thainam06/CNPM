-- READ ONLY: send the results to the application developer, not database secrets.
SELECT table_schema, table_name, column_name, data_type, is_nullable
FROM information_schema.columns
WHERE table_schema NOT IN ('pg_catalog','information_schema','auth','storage','extensions',
  'realtime','vault','graphql','graphql_public','supabase_functions','supabase_migrations')
ORDER BY table_schema, table_name, ordinal_position;

SELECT n.nspname AS schema_name, c.relname AS table_name, k.conname,
       pg_get_constraintdef(k.oid) AS constraint_definition, k.condeferrable
FROM pg_constraint k JOIN pg_class c ON c.oid=k.conrelid
JOIN pg_namespace n ON n.oid=c.relnamespace
WHERE n.nspname IN ('smarttour_demo','public')
ORDER BY n.nspname,c.relname,k.conname;
