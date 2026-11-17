#!/usr/bin/env bash
# Disposable Compose project: never use the demo's containers or volumes.
set -euo pipefail
cd "$(dirname "$0")/.."
verification_dir=$(mktemp -d)
verification_project="smarttour-verify-$(basename "$verification_dir" | tr '[:upper:]' '[:lower:]' | tr -cd 'a-z0-9')"
python3 - "$verification_dir/runtime.env" <<'PY'
import os, secrets, socket, sys
with socket.socket() as listener:
    listener.bind(('127.0.0.1', 0))
    port = listener.getsockname()[1]
with open(sys.argv[1], 'x') as output:
    output.write(f'SMARTTOUR_PORT={port}\nSMARTTOUR_JWT_SECRET={secrets.token_hex(32)}\nSMARTTOUR_ADMIN_PASSWORD={secrets.token_urlsafe(30)}\nSMARTTOUR_STORAGE_PROVIDER=Sqlite\n')
os.chmod(sys.argv[1], 0o600)
PY
compose=(docker compose -p "$verification_project" --env-file "$verification_dir/runtime.env" -f compose.yaml)
cleanup() {
    # Only this newly generated test project's volumes; never the demo volumes.
    "${compose[@]}" down -v --remove-orphans >/dev/null 2>&1 || true
    rm -rf "$verification_dir"
}
trap cleanup EXIT
"${compose[@]}" up --build -d --wait --wait-timeout 240
verification_container=$("${compose[@]}" ps -q api)
# Sentinel lives in the mounted data volume, not the container filesystem.
docker exec "$verification_container" python3 -c "from pathlib import Path; Path('/app/App_Data/restart-verification.txt').write_text('isolated-volume-check')"
verification_port=$(sed -n 's/^SMARTTOUR_PORT=//p' "$verification_dir/runtime.env")
python3 - "$verification_port" "$verification_dir/catalog.json" <<'PY'
import json, sys, urllib.request
base = 'http://127.0.0.1:' + sys.argv[1]
with urllib.request.urlopen(base + '/api/health/ready', timeout=20) as response:
    assert json.load(response)['status'] == 'ready'
with urllib.request.urlopen(base + '/api/places', timeout=20) as response:
    places = json.load(response)
    assert len(places) > 0
    with open(sys.argv[2], 'w') as output: json.dump(places, output)
PY
verification_sql="import hashlib,sqlite3; db=sqlite3.connect('file:/app/App_Data/smarttour.db?mode=ro',uri=True); print(hashlib.sha256(repr(db.execute('SELECT * FROM users ORDER BY ordinal').fetchall()).encode()).hexdigest())"
docker exec "$verification_container" python3 -c "$verification_sql" > "$verification_dir/users.sha256"
# Recreate containers to prove state is in volumes, beyond a process-only restart.
"${compose[@]}" up -d --force-recreate --wait --wait-timeout 180
verification_container=$("${compose[@]}" ps -q api)
docker exec "$verification_container" python3 -c "$verification_sql" > "$verification_dir/users-after.sha256"
cmp "$verification_dir/users.sha256" "$verification_dir/users-after.sha256"
docker exec "$verification_container" python3 -c "from pathlib import Path; assert Path('/app/App_Data/restart-verification.txt').read_text() == 'isolated-volume-check'"
python3 - "$verification_port" "$verification_dir/catalog.json" <<'PY'
import json, sys, urllib.request
with urllib.request.urlopen('http://127.0.0.1:' + sys.argv[1] + '/api/places', timeout=20) as response:
    with open(sys.argv[2]) as original: assert json.load(response) == json.load(original)
PY
printf '%s\n' 'PASS: Docker health, catalog, account rows and volume data survive container recreation. Physical host reboot remains a manual check.'
