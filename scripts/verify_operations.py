"""Run real HTTP/SQLite/restart/restore checks on disposable data, never the demo DB.
Requires Release build, dotnet on PATH and ffmpeg. No physical-device claims.
"""
from contextlib import closing, contextmanager
from datetime import datetime, timezone
import concurrent.futures
import json
import os
from pathlib import Path
import secrets
import socket
import sqlite3
import subprocess
import tempfile
import time
import urllib.error
import urllib.request
import uuid

from backup_restore import backup, restore, digest

ROOT = Path(__file__).resolve().parents[1]
DLL = ROOT / 'ShortVideoTranslator.Api/bin/Release/net10.0/ShortVideoTranslator.Api.dll'
# Explicit direct loopback avoids inheriting outbound proxy settings for local checks.
HTTP = urllib.request.build_opener(urllib.request.ProxyHandler({}))


def request(base, path, payload=None, token=None):
    headers = {'Content-Type': 'application/json'}
    if token:
        headers['Authorization'] = 'Bearer ' + token
    req = urllib.request.Request(base + path, data=json.dumps(payload).encode() if payload is not None else None, headers=headers)
    with HTTP.open(req, timeout=15) as response:
        body = response.read()
        return json.loads(body) if body else None


@contextmanager
def api(data, password, log):
    with socket.socket() as listener:
        listener.bind(('127.0.0.1', 0))
        port = listener.getsockname()[1]
    base = f'http://127.0.0.1:{port}'
    env = os.environ.copy()
    env.update(ASPNETCORE_ENVIRONMENT='Development', ASPNETCORE_URLS=base,
               Storage__Directory=str(data), Storage__Provider='Sqlite',
               Admin__BootstrapPassword=password, Audio__WorkersEnabled='false')
    with log.open('ab') as output:
        process = subprocess.Popen(['dotnet', str(DLL)], cwd=ROOT / 'ShortVideoTranslator.Api', env=env, stdout=output, stderr=output)
        try:
            for _ in range(100):
                if process.poll() is not None:
                    raise RuntimeError('API exited before readiness; inspect private log')
                try:
                    if request(base, '/api/health/ready')['status'] == 'ready':
                        break
                except (urllib.error.URLError, OSError):
                    pass
                time.sleep(.2)
            else:
                raise RuntimeError('API readiness timed out')
            yield base
        finally:
            if process.poll() is None:
                process.terminate()
            try:
                process.wait(timeout=15)
            except subprocess.TimeoutExpired:
                process.kill()
                process.wait()


def login(base, password):
    return request(base, '/api/auth/token', {'username': 'admin', 'password': password})['accessToken']


def main():
    if not DLL.is_file():
        raise RuntimeError('Build the Release solution first')
    with tempfile.TemporaryDirectory(prefix='smarttour-operations-') as temporary:
        root = Path(temporary)
        data = root / 'source'
        initial = secrets.token_urlsafe(30)
        changed = secrets.token_urlsafe(30)
        devices = [str(uuid.uuid4()) for _ in range(3)]
        descriptors = [dict(browser='Chrome', operatingSystem='Android', type='Mobile'),
                       dict(browser='Safari', operatingSystem='iOS', type='Mobile'),
                       dict(browser='Firefox', operatingSystem='Linux', type='Desktop')]
        with api(data, initial, root / 'source.log') as base:
            token = login(base, initial)
            request(base, '/api/auth/change-password', {'currentPassword': initial, 'newPassword': changed}, token)
            token = login(base, changed)
            catalog = request(base, '/api/places')
            assert len(catalog) > 0
            place = catalog[0]['id']

            def send(index):
                now = datetime.now(timezone.utc).isoformat()
                events = [dict(id=str(uuid.uuid4()), kind=kind, placeId=None if kind == 'heartbeat' else place, language='vi', occurredAt=now) for kind in ('heartbeat', 'view', 'play')]
                batch = dict(consent=True, deviceId=devices[index], device=descriptors[index], events=events)
                request(base, '/api/analytics/batch', batch)
                request(base, '/api/analytics/batch', batch)  # Retry must not double metrics.
                request(base, '/api/presence/heartbeat', dict(deviceId=devices[index], sessionId=devices[index], device=descriptors[index]))

            with concurrent.futures.ThreadPoolExecutor(max_workers=3) as pool:
                list(pool.map(send, range(3)))
            for _ in range(50):
                report = request(base, '/api/admin/devices', token=token)
                if report['total'] == 3 and all(item['plays'] == 1 for item in report['items']):
                    break
                time.sleep(.2)
            else:
                raise AssertionError('Analytics worker did not process expected events')
            assert report['online'] == 3
            for index, device in enumerate(devices):
                item = next(item for item in report['items'] if item['deviceId'] == device)
                assert item['views'] == item['plays'] == 1
                assert item['descriptor'] == descriptors[index]
            assert request(base, '/api/admin/presence', token=token)['online'] == 3
            request(base, '/api/presence/leave', dict(deviceId=devices[0], sessionId=devices[0]))
            assert request(base, '/api/admin/presence', token=token)['online'] == 2
            request(base, '/api/analytics/forget', dict(deviceId=devices[0]))
            assert request(base, '/api/admin/devices', token=token)['total'] == 2
            with closing(sqlite3.connect(data / 'smarttour.db')) as db:
                for table in ('events', 'pendingAnalytics', 'analyticsHeartbeats', 'analyticsDevices'):
                    assert db.execute(f'SELECT count(*) FROM {table} WHERE deviceId=?', (devices[0],)).fetchone()[0] == 0
            print('PASS: 3 concurrent synthetic HTTP clients, descriptors, view/play deduplication, protected Admin reports, leave and SQL forget')
        # The API is now stopped: create a real decodable MP3 fixture, not live speech.
        audio = data / 'audio' / 'verification-tone.mp3'
        audio.parent.mkdir(exist_ok=True)
        subprocess.run(['ffmpeg', '-loglevel', 'error', '-f', 'lavfi', '-i', 'sine=frequency=440:duration=1', str(audio)], check=True)
        keys = Path.home() / '.aspnet' / 'DataProtection-Keys'
        snapshot = root / 'backup'
        manifest = backup(data, snapshot, keys if keys.is_dir() else None)
        restored = root / 'restored'
        restore(snapshot, restored)
        assert digest(audio) == digest(restored / 'App_Data/audio/verification-tone.mp3')
        # Actual second API instance opens the restored SQL database and authenticates.
        with api(restored / 'App_Data', secrets.token_urlsafe(30), root / 'restored.log') as base:
            token = login(base, changed)  # Original changed password, not new bootstrap value.
            assert request(base, '/api/places') == catalog
            report = request(base, '/api/admin/devices', token=token)
            assert report['total'] == 2
        print(f'PASS: full backup/restore, {len(manifest["files"])} file checksums, {len(manifest["tables"])} SQL table counts, existing account login, catalog, analytics and MP3')
        with api(data, initial, root / 'restart.log') as base:
            login(base, changed)
            assert request(base, '/api/places') == catalog
        print('PASS: process restart preserves source database and changed account password')
        print('NOT TESTED: physical phones/GPS, real speech pronunciation, host reboot and Docker persistence')


if __name__ == '__main__':
    main()
