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
    docker = folder / 'docker'
    docker.write_text('''#!/usr/bin/env python3
import json, os, sys
args = sys.argv[1:]
with open(os.environ['MOCK_CALLS'], 'a') as output: output.write(json.dumps(args) + '\\n')
if args[0] == 'info': print('linux')
elif '--CheckPostgres=true' in args:
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
        for failing in (False, True):
            log.write_text('')
            env = os.environ.copy()
            env.update(PATH=str(folder) + os.pathsep + env['PATH'], MOCK_CALLS=str(log),
                       MOCK_FAILURE='1' if failing else '0', MOCK_PORT=str(server.server_port),
                       XDG_CACHE_HOME=str(folder/'cache'), XDG_CONFIG_HOME=str(folder/'config'), XDG_DATA_HOME=str(folder/'data'))
            run = subprocess.run([options.pwsh, '-NoProfile', '-File', str(root/'scripts/Start-Supabase.ps1'),
                                  '-EnvFile', str(envfile)], env=env, capture_output=True, text=True, timeout=60)
            calls = [json.loads(line) for line in log.read_text().splitlines()]
            output = run.stdout + run.stderr
            assert 'private-fixture-value' not in output
            assert all(any(x.endswith('compose.supabase.yaml') for x in call) for call in calls if call[0] == 'compose')
            assert not any('--ImportPostgres=true' in call for call in calls)
            assert any('--CheckPostgres=true' in call and '--ImportPostgres=false' in call for call in calls)
            if failing:
                assert run.returncode != 0 and 'CONNECT' in output
                assert not any('up' in call for call in calls), 'Do not start web after a failed database check'
            else:
                assert run.returncode == 0, output
                assert 'PASS: web + PostgreSQL are ready' in output
                assert any('up' in call and '--force-recreate' in call for call in calls)
            print('PASS: launcher ' + ('stops on database failure' if failing else 'verifies cloud web before success'))
    finally:
        server.shutdown()
        server.server_close()
