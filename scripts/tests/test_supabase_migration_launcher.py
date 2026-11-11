"""Real PowerShell + fake Docker; never connects to Supabase or starts a DB."""
import argparse
import json
import os
from pathlib import Path
import subprocess
import tempfile

parser = argparse.ArgumentParser()
parser.add_argument('--pwsh', default='pwsh')
options = parser.parse_args()
root = Path(__file__).resolve().parents[2]
with tempfile.TemporaryDirectory(prefix='supabase-migration-launcher-') as temporary:
    folder = Path(temporary)
    envfile = folder / '.env.docker'
    envfile.write_text("SUPABASE_CONNECTION_STRING='private-migration-fixture'\n")
    certificate = folder / 'public ca fixture.crt'
    certificate.write_text('public fixture only')
    log = folder / 'calls.jsonl'
    docker = folder / 'docker'
    docker.write_text('''#!/usr/bin/env python3
import json, os, sys
args=sys.argv[1:]
with open(os.environ['MOCK_CALLS'],'a') as output: output.write(json.dumps(args)+'\\n')
if args[0]=='info': print('linux')
elif '--MigratePostgresSixTables=true' in args:
    if os.environ['MOCK_FAILURE']=='1':
        print('{"success":false,"code":"MAPPING_REQUIRED","blockers":["fixture"]}')
        sys.exit(2)
    print('{"success":true,"code":"PLAN_READY"}')
''')
    docker.chmod(0o755)
    for apply, backup, accept, failed in ((False, False, False, False), (True, False, False, False),
                                         (True, True, True, False), (False, False, False, True)):
        log.write_text('')
        env = os.environ.copy()
        env.update(PATH=str(folder)+os.pathsep+env['PATH'], MOCK_CALLS=str(log), MOCK_FAILURE='1' if failed else '0',
                   XDG_CACHE_HOME=str(folder/'cache'), XDG_CONFIG_HOME=str(folder/'config'), XDG_DATA_HOME=str(folder/'data'))
        args = [options.pwsh, '-NoProfile', '-File', str(root/'scripts/Migrate-Supabase.ps1'),
                '-EnvFile', str(envfile), '-CertificateFile', str(certificate)]
        if apply: args.append('-Apply')
        if backup: args.append('-BackupConfirmed')
        if accept: args.append('-AcceptCompactSnapshot')
        run = subprocess.run(args, env=env, capture_output=True, text=True, timeout=60)
        output = run.stdout+run.stderr
        assert 'private-migration-fixture' not in output
        calls = [json.loads(line) for line in log.read_text().splitlines()]
        if apply and not backup:
            assert run.returncode != 0 and not calls
            print('PASS: missing backup confirmation stops before Docker')
            continue
        assert all('up' not in call and '--ImportPostgres=true' not in call for call in calls)
        command = next(call for call in calls if '--MigratePostgresSixTables=true' in call)
        assert '--ImportPostgres=false' in command and '--CheckPostgres=false' in command
        assert str(certificate)+':/app/App_Data/supabase-ca.crt:ro' in command
        assert '--Storage:PostgresRootCertificate=/app/App_Data/supabase-ca.crt' in command
        assert f'--ApplyPostgresMigration={str(apply).lower()}' in command
        assert f'--AcceptCompactSnapshot={str(accept).lower()}' in command
        assert (run.returncode != 0) == failed
        if failed: assert 'MAPPING_REQUIRED' in output
        print('PASS:', 'apply' if apply else 'plan', 'failure stops' if failed else 'arguments and read-only CA mount')
