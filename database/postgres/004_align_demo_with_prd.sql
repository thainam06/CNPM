-- PRD 22/10/2026, sections I.9 and VI.2-VI.8: physical SQL mapping.
-- Run AFTER 002 (compact design) or 003 (conversion of the earlier design).
-- Additive migration of smarttour_demo: no DROP, no new schema, no API switch.
BEGIN;
SET LOCAL lock_timeout = '10s';
DO $$ BEGIN
  IF to_regclass('smarttour_demo.visitor_sessions') IS NULL
     OR NOT EXISTS (SELECT 1 FROM information_schema.columns
                    WHERE table_schema='smarttour_demo' AND table_name='places'
                    AND column_name='settings') THEN
    RAISE EXCEPTION 'Expected the compact smarttour_demo design. Apply 002/003 first.';
  END IF;
END $$;

-- PlatformRole: PRD permits permissions[] in the role itself.
CREATE TABLE IF NOT EXISTS smarttour_demo.roles (
  name text PRIMARY KEY,
  permissions text[] NOT NULL DEFAULT '{}',
  is_system boolean NOT NULL DEFAULT false
);
-- Preserve role names already in users; do not invent or grant permissions.
INSERT INTO smarttour_demo.roles(name)
SELECT DISTINCT role FROM smarttour_demo.users ON CONFLICT DO NOTHING;
DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_constraint
      WHERE conrelid='smarttour_demo.users'::regclass AND conname='users_role_fk') THEN
    ALTER TABLE smarttour_demo.users ADD CONSTRAINT users_role_fk
      FOREIGN KEY(role) REFERENCES smarttour_demo.roles(name);
  END IF;
END $$;

-- Security-critical properties are explicit columns, not opaque JSON.
ALTER TABLE smarttour_demo.users
  ADD COLUMN IF NOT EXISTS security_stamp text,
  ADD COLUMN IF NOT EXISTS verified boolean,
  ADD COLUMN IF NOT EXISTS must_change_password boolean;
UPDATE smarttour_demo.users SET
  security_stamp=COALESCE(security_stamp, settings->>'security_stamp'),
  verified=COALESCE(verified, (settings->>'verified')::boolean, false),
  must_change_password=COALESCE(must_change_password, (settings->>'must_change_password')::boolean, false);
ALTER TABLE smarttour_demo.users
  ALTER COLUMN verified SET DEFAULT false, ALTER COLUMN verified SET NOT NULL,
  ALTER COLUMN must_change_password SET DEFAULT false, ALTER COLUMN must_change_password SET NOT NULL;

-- Main POI data from PRD I.9/VI.6. Source text/hash remain nullable until
-- real content is imported; never replace missing content with fabricated data.
ALTER TABLE smarttour_demo.places
  ADD COLUMN IF NOT EXISTS category text,
  ADD COLUMN IF NOT EXISTS description_vi text,
  ADD COLUMN IF NOT EXISTS source_hash text,
  ADD COLUMN IF NOT EXISTS source_url text,
  ADD COLUMN IF NOT EXISTS is_demo boolean,
  ADD COLUMN IF NOT EXISTS is_active boolean,
  ADD COLUMN IF NOT EXISTS location_verified boolean,
  ADD COLUMN IF NOT EXISTS updated_at timestamptz;
UPDATE smarttour_demo.places SET
  category=COALESCE(category, settings->>'category'),
  description_vi=COALESCE(description_vi, settings->>'narration_vi'),
  source_hash=COALESCE(source_hash, settings->>'source_hash'),
  source_url=COALESCE(source_url, settings->>'source_url'),
  is_demo=COALESCE(is_demo, (settings->>'is_demo')::boolean, true),
  is_active=COALESCE(is_active, (settings->>'is_active')::boolean, false),
  location_verified=COALESCE(location_verified, (settings->>'location_verified')::boolean, false),
  updated_at=COALESCE(updated_at, (settings->>'updated_at')::timestamptz);
ALTER TABLE smarttour_demo.places
  ALTER COLUMN is_demo SET DEFAULT true, ALTER COLUMN is_demo SET NOT NULL,
  ALTER COLUMN is_active SET DEFAULT false, ALTER COLUMN is_active SET NOT NULL,
  ALTER COLUMN location_verified SET DEFAULT false, ALTER COLUMN location_verified SET NOT NULL;

ALTER TABLE smarttour_demo.place_images
  ADD COLUMN IF NOT EXISTS credit text,
  ADD COLUMN IF NOT EXISTS source_url text,
  ADD COLUMN IF NOT EXISTS license text,
  ADD COLUMN IF NOT EXISTS focus_y integer CHECK(focus_y BETWEEN 0 AND 100);
UPDATE smarttour_demo.place_images SET
  credit=COALESCE(credit, metadata->>'credit'),
  source_url=COALESCE(source_url, metadata->>'source_url'),
  license=COALESCE(license, metadata->>'license'),
  focus_y=COALESCE(focus_y, (metadata->>'focus_y')::integer);

-- TranslationDraft + approved revision are states of a versioned narration.
ALTER TABLE smarttour_demo.place_narrations
  ADD COLUMN IF NOT EXISTS source_hash text,
  ADD COLUMN IF NOT EXISTS provider text,
  ADD COLUMN IF NOT EXISTS published_revision text,
  ADD COLUMN IF NOT EXISTS updated_at timestamptz;
UPDATE smarttour_demo.place_narrations SET
  source_hash=COALESCE(source_hash, metadata->>'source_hash'),
  provider=COALESCE(provider, metadata->>'provider'),
  published_revision=COALESCE(published_revision, metadata->>'published_revision'),
  updated_at=COALESCE(updated_at, (metadata->>'updated_at')::timestamptz);

CREATE TABLE IF NOT EXISTS smarttour_demo.menu_items (
  id text PRIMARY KEY,
  place_id bigint NOT NULL REFERENCES smarttour_demo.places(id),
  name text NOT NULL,
  description text NOT NULL DEFAULT '',
  price_text text,
  price numeric(14,2) CHECK(price >= 0)
);

-- Owner registration and POI proposals have different lifecycles/ownership.
CREATE TABLE IF NOT EXISTS smarttour_demo.owner_requests (
  id text PRIMARY KEY,
  user_id text NOT NULL REFERENCES smarttour_demo.users(id),
  status text NOT NULL CHECK(status IN ('pending','approved','rejected')),
  applicant_note text NOT NULL DEFAULT '',
  review_note text NOT NULL DEFAULT '',
  submitted_at timestamptz NOT NULL,
  reviewed_at timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS one_pending_owner_request
  ON smarttour_demo.owner_requests(user_id) WHERE status='pending';
CREATE TABLE IF NOT EXISTS smarttour_demo.place_proposals (
  id text PRIMARY KEY,
  submitted_by text NOT NULL REFERENCES smarttour_demo.users(id),
  place_id bigint REFERENCES smarttour_demo.places(id),
  kind text NOT NULL CHECK(kind IN ('create','update')),
  payload jsonb NOT NULL CHECK(jsonb_typeof(payload)='object'),
  status text NOT NULL CHECK(status IN ('pending','approved','rejected')),
  applicant_note text NOT NULL DEFAULT '',
  review_note text NOT NULL DEFAULT '',
  expected_place_version timestamptz,
  submitted_at timestamptz NOT NULL,
  reviewed_at timestamptz,
  CHECK(kind <> 'update' OR place_id IS NOT NULL)
);
CREATE UNIQUE INDEX IF NOT EXISTS one_pending_update_per_place
  ON smarttour_demo.place_proposals(place_id) WHERE status='pending' AND kind='update';
CREATE TABLE IF NOT EXISTS smarttour_demo.notifications (
  id text PRIMARY KEY,
  recipient_user_id text NOT NULL REFERENCES smarttour_demo.users(id),
  owner_request_id text REFERENCES smarttour_demo.owner_requests(id),
  place_proposal_id text REFERENCES smarttour_demo.place_proposals(id),
  message text NOT NULL,
  is_read boolean NOT NULL DEFAULT false,
  created_at timestamptz NOT NULL,
  CHECK(num_nonnulls(owner_request_id, place_proposal_id) <= 1)
);

-- MP3 metadata only. File bytes stay in App_Data or object storage.
CREATE TABLE IF NOT EXISTS smarttour_demo.audio_assets (
  content_key text PRIMARY KEY,
  place_id bigint NOT NULL REFERENCES smarttour_demo.places(id),
  language_code text NOT NULL CHECK(language_code IN ('vi','en','zh','ja','ko','fr')),
  source_hash text NOT NULL,
  revision text NOT NULL,
  file_path text NOT NULL,
  checksum text NOT NULL,
  voice text NOT NULL,
  provider text NOT NULL,
  created_at timestamptz NOT NULL,
  UNIQUE(place_id, language_code, source_hash, revision)
);
CREATE TABLE IF NOT EXISTS smarttour_demo.audio_tasks (
  id text PRIMARY KEY,
  place_id bigint NOT NULL REFERENCES smarttour_demo.places(id),
  languages text[] NOT NULL CHECK(cardinality(languages) BETWEEN 1 AND 6
    AND languages <@ ARRAY['vi','en','zh','ja','ko','fr']::text[]
    AND array_position(languages, NULL) IS NULL),
  source_hash text NOT NULL,
  status text NOT NULL CHECK(status IN ('queued','running','paused','completed','failed','cancelled')),
  completed integer NOT NULL DEFAULT 0 CHECK(completed >= 0 AND completed <= cardinality(languages)),
  failure_reason text,
  requested_by text NOT NULL REFERENCES smarttour_demo.users(id),
  created_at timestamptz NOT NULL,
  updated_at timestamptz NOT NULL,
  heartbeat_at timestamptz
);
CREATE UNIQUE INDEX IF NOT EXISTS one_active_audio_task_per_place
  ON smarttour_demo.audio_tasks(place_id) WHERE status IN ('queued','running','paused');

CREATE TABLE IF NOT EXISTS smarttour_demo.audit_logs (
  id text PRIMARY KEY,
  actor_id text NOT NULL,
  action text NOT NULL,
  resource text NOT NULL,
  created_at timestamptz NOT NULL
);
-- Runtime GPS opt-in is independent of operational visitor_sessions.
CREATE TABLE IF NOT EXISTS smarttour_demo.runtime_sessions (
  session_id text PRIMARY KEY,
  consent_id text NOT NULL,
  rounded_latitude double precision NOT NULL CHECK(rounded_latitude BETWEEN -90 AND 90),
  rounded_longitude double precision NOT NULL CHECK(rounded_longitude BETWEEN -180 AND 180),
  updated_at timestamptz NOT NULL,
  expires_at timestamptz NOT NULL,
  CHECK(expires_at > updated_at AND expires_at <= updated_at + interval '24 hours')
);

-- Registry is fixed to these 15 known tables; do not modify other schemas.
DO $$ DECLARE t text; BEGIN
  FOREACH t IN ARRAY ARRAY['roles','users','places','place_images','place_narrations',
    'menu_items','owner_requests','place_proposals','notifications','audio_assets',
    'audio_tasks','audit_logs','visitor_sessions','activity_logs','runtime_sessions'] LOOP
    EXECUTE format('ALTER TABLE smarttour_demo.%I ENABLE ROW LEVEL SECURITY',t);
    EXECUTE format('REVOKE ALL ON TABLE smarttour_demo.%I FROM PUBLIC',t);
  END LOOP;
END $$;
REVOKE ALL ON SCHEMA smarttour_demo FROM PUBLIC;
COMMIT;

SELECT table_name FROM information_schema.tables
WHERE table_schema='smarttour_demo' AND table_type='BASE TABLE' ORDER BY table_name;
