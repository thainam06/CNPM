import importlib.util
from pathlib import Path
import sqlite3
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('backup_restore', Path(__file__).parents[1] / 'backup_restore.py')
backup = importlib.util.module_from_spec(spec)
spec.loader.exec_module(backup)


class BackupRestoreTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        self.data = self.root / 'data'
        self.data.mkdir()
        self.keys = self.root / 'keys'
        self.keys.mkdir()
        (self.keys / 'key.xml').write_text('test key fixture')
        (self.data / 'audio').mkdir()
        (self.data / 'audio' / 'fixture.mp3').write_bytes(b'fixture MP3 bytes')
        self.db = sqlite3.connect(self.data / 'smarttour.db')
        self.addCleanup(self.db.close)
        self.db.execute('PRAGMA journal_mode=WAL')
        self.db.execute('CREATE TABLE users(id TEXT, passwordHash TEXT)')
        self.db.execute("INSERT INTO users VALUES ('fixture','fixture password hash')")
        self.db.commit()
        self.snapshot = self.root / 'backup'

    def test_wal_restore_preserves_accounts_media_keys_and_is_independent(self):
        backup.backup(self.data, self.snapshot, self.keys)
        target = self.root / 'restored'
        backup.restore(self.snapshot, target)
        with sqlite3.connect(target / 'App_Data' / 'smarttour.db') as restored:
            self.assertEqual(restored.execute('SELECT * FROM users').fetchall(), self.db.execute('SELECT * FROM users').fetchall())
            restored.execute("DELETE FROM users")
        self.assertEqual(self.db.execute('SELECT count(*) FROM users').fetchone()[0], 1)
        self.assertEqual((target / 'App_Data/audio/fixture.mp3').read_bytes(), b'fixture MP3 bytes')
        self.assertEqual((target / 'DataProtection-Keys/key.xml').read_bytes(), (self.keys / 'key.xml').read_bytes())

    def test_corrupt_or_extra_files_prevent_restore_without_partial_destination(self):
        backup.backup(self.data, self.snapshot)
        (self.snapshot / 'App_Data/audio/fixture.mp3').write_bytes(b'changed')
        with self.assertRaises(ValueError):
            backup.restore(self.snapshot, self.root / 'restored')
        self.assertFalse((self.root / 'restored').exists())

    def test_existing_destination_and_nested_destination_are_rejected(self):
        with self.assertRaises(ValueError):
            backup.backup(self.data, self.data / 'nested')
        backup.backup(self.data, self.snapshot)
        with self.assertRaises(ValueError):
            backup.restore(self.snapshot, self.data)

    def test_symlink_cannot_copy_external_secret(self):
        (self.data / 'external').symlink_to(self.keys / 'key.xml')
        with self.assertRaises(ValueError):
            backup.backup(self.data, self.snapshot)

    def test_extra_file_rejected(self):
        backup.backup(self.data, self.snapshot)
        (self.snapshot / 'extra').write_text('unexpected')
        with self.assertRaises(ValueError):
            backup.verify(self.snapshot)


if __name__ == '__main__':
    unittest.main()
