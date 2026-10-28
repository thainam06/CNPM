"""Integration probe inside API container. Creates and removes only random test devices."""
import concurrent.futures
import datetime
import json
import sqlite3
import sys
import time
import urllib.request
import uuid

base = sys.argv[1]
devices = [str(uuid.uuid4()) for _ in range(12)]
def post(path, payload):
    req = urllib.request.Request(base + path, data=json.dumps(payload).encode(), headers={'Content-Type': 'application/json'})
    with urllib.request.urlopen(req, timeout=30) as response:
        return response.status
def send(device):
    return post('/api/analytics/batch', {'consent': True, 'deviceId': device, 'events': [{'id': str(uuid.uuid4()), 'kind': 'heartbeat', 'placeId': None, 'language': 'vi', 'occurredAt': datetime.datetime.now(datetime.timezone.utc).isoformat()}]})
placeholders = ','.join('?' for _ in devices)
with sqlite3.connect('/app/App_Data/smarttour.db') as db:
    try:
        with concurrent.futures.ThreadPoolExecutor(max_workers=12) as pool:
            assert all(code == 202 for code in pool.map(send, devices))
        count = 0
        for _ in range(15):
            count = db.execute(f'SELECT count(DISTINCT deviceId) FROM analyticsHeartbeats WHERE deviceId IN ({placeholders})', devices).fetchone()[0]
            if count == 12:
                break
            time.sleep(1)
        assert count == 12, count
        print('PASS: 12 concurrent HTTPS clients -> Cloudflare -> API -> SQLite heartbeats.')
    finally:
        for device in devices:
            post('/api/analytics/forget', {'deviceId': device})
    for table in ['events', 'pendingAnalytics', 'analyticsHeartbeats']:
        assert db.execute(f'SELECT count(*) FROM {table} WHERE deviceId IN ({placeholders})', devices).fetchone()[0] == 0
    print('PASS: synthetic device events removed by forget endpoint; real visitors untouched.')
