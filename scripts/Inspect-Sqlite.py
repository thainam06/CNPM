"""Run inside the API container: python3 /tmp/Inspect-Sqlite.py [backup-path]."""
import sqlite3
import sys

with sqlite3.connect('/app/App_Data/smarttour.db') as db:
    if len(sys.argv) > 1:
        with sqlite3.connect(sys.argv[1]) as backup:
            db.backup(backup)
        print('Consistent SQLite backup created.')
    print('Integrity:', db.execute('PRAGMA integrity_check').fetchone()[0])
    print('Tables:', ', '.join(row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")))
    print('POIs:', db.execute('SELECT count(*) FROM places').fetchone()[0])
    print('Consented browsers retained:', db.execute('SELECT count(DISTINCT deviceId) FROM events').fetchone()[0])
    print('Active within 5 minutes:', db.execute("SELECT count(DISTINCT deviceId) FROM analyticsHeartbeats WHERE julianday(lastSeen)>julianday('now','-5 minutes')").fetchone()[0])
