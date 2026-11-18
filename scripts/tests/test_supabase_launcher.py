"""Launch real PowerShell with a fake Docker CLI/local HTTP fixture; no Docker data touched."""
import argparse
import http.server
import json
import os
from pathlib import Path
import subprocess
import tempfile
import threading

parser = argparse.ArgumentParser()
parser.add_argument('--pwsh', default='pwsh')
options = parser.parse_args()
root = Path(__file__).resolve().parents[2]

class Handler(http.server.BaseHTTPRequestHandler):
    def do_GET(self):
        data = {'storage': 'postgresql'} if self.path == '/api/health' else {'status': 'ready'}
        self.send_response(200)
        self.send_header('Content-Type', 'application/json')
        self.end_headers()
        self.wfile.write(json.dumps(data).encode())
    def log_message(self, *_):
        pass

with tempfile.TemporaryDirectory(prefix='supabase-launcher-') as tmp:
    folder = Path(tmp)
    envfile = folder / '.env.docker'
    envfile.write_text("SUPABASE_CONNECTION_STRING='private-fixture-value'\n")
    log = folder / 'calls.jsonl'
    certificate = folder / 'public fixture ca.crt'
    certificate.write_text('public certificate fixture; not used for actual TLS')
    docker = folder / 'docker'
    docker.write_text('''#!/usr/bin/env python3
import json, os, sys
args = sys.argv[1:]
with open(os.environ['MOCK_CALLS'], 'a') as output: output.write(json.dumps(args) + '\\n')
if args[0] == 'info': print('linux')
elif '--CheckPostgres=true' in args or '--ReviewPostgres=true' in args:
    if os.environ['MOCK_FAILURE'] == '1':
        print('{"ready":false,"code":"CONNECT"}')
        sys.exit(2)
    print('{"ready":true,"code":"READY"}')
elif 'port' in args: print('127.0.0.1:' + os.environ['MOCK_PORT'])
''')
    docker.chmod(0o755)
    server = http.server.ThreadingHTTPServer(('127.0.0.1', 0), Handler)
    thread = threading.Thread(target=server.serve_forever, daemon=True)
    thread.start()
    try:
        for failing, review, check_only, use_certificate in (
            (False, False, False, False), (True, False, False, False),
            (False, True, False, False), (True, True, False, False),
            (False, True, False, True), (False, False, True, True),
            (False, False, False, True), (False, False, True, False),
        ):
            log.write_text('')
            env = os.environ.copy()
            env.update(PATH=str(folder) + os.pathsep + env['PATH'], MOCK_CALLS=str(log),
                       MOCK_FAILURE='1' if failing else '0', MOCK_PORT=str(server.server_port),
                       XDG_CACHE_HOME=str(folder/'cache'), XDG_CONFIG_HOME=str(folder/'config'), XDG_DATA_HOME=str(folder/'data'))
            run = subprocess.run([options.pwsh, '-NoProfile', '-File', str(root/'scripts/Start-Supabase.ps1'),
                                  '-EnvFile', str(envfile)] + (['-ReviewOnly', '-IncludeConnectionInfo'] if review else (['-CheckOnly'] if check_only else [])) + (['-CertificateFile', str(certificate)] if use_certificate else []), env=env, capture_output=True, text=True, timeout=60)
            calls = [json.loads(line) for line in log.read_text().splitlines()]
            output = run.stdout + run.stderr
            assert 'private-fixture-value' not in output
            assert all(any(x.endswith('compose.supabase.yaml') for x in call) for call in calls if call[0] == 'compose')
            assert not any('--ImportPostgres=true' in call for call in calls)
            assert any(('--ReviewPostgres=true' if review else '--CheckPostgres=true') in call and '--ImportPostgres=false' in call for call in calls)
            if review:
                assert any('--IncludeConnectionInfo=true' in call for call in calls)
            if review or check_only:
                assert not any('up' in call for call in calls), 'Diagnostics must never start web/workers'
                assert not any('--entrypoint' in call for call in calls), 'Diagnostics must never copy certificates into the volume'
            diagnostic = next(call for call in calls if '--ImportPostgres=false' in call)
            if use_certificate:
                assert str(certificate) + ':/app/App_Data/supabase-ca.crt:ro' in diagnostic
                assert '--Storage:PostgresRootCertificate=/app/App_Data/supabase-ca.crt' in diagnostic
            else:
                assert not any(arg.startswith('--Storage:PostgresRootCertificate=') for arg in diagnostic)
            if failing:
                assert run.returncode != 0 and 'CONNECT' in output
                assert not any('up' in call for call in calls), 'Do not start web after a failed database check'
            else:
                assert run.returncode == 0, output
                if not review and not check_only:
                    assert 'PASS: web + PostgreSQL are ready' in output
                    assert any('up' in call and '--force-recreate' in call for call in calls)
            print('PASS:', 'review' if review else ('check-only' if check_only else 'startup'), 'with CA' if use_certificate else 'without CA', 'failure stops' if failing else 'success')
        log.write_text('')
        missing = subprocess.run([options.pwsh, '-NoProfile', '-File', str(root/'scripts/Start-Supabase.ps1'),
                                  '-EnvFile', str(envfile), '-ReviewOnly', '-CertificateFile', str(folder/'missing.crt')],
                                 env=env, capture_output=True, text=True, timeout=60)
        assert missing.returncode != 0
        assert 'specified CA certificate file does not exist' in missing.stdout + missing.stderr
        assert log.read_text() == '', 'Reject a missing CA before invoking Docker'
        assert 'private-fixture-value' not in missing.stdout + missing.stderr
        print('PASS: missing explicit CA rejected before build or connection')
    finally:
        server.shutdown()
        server.server_close()
