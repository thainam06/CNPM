"""Private, verified full-directory backups. Stop the API before backup/restore.

No credentials or record payloads are printed. Restore always targets a new directory.
"""
import argparse
from contextlib import closing
import hashlib
import json
from pathlib import Path
import shutil
import sqlite3
import tempfile


def digest(path):
    with path.open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def files(root):
    result = {}
    for path in sorted(root.rglob('*')):
        if path.is_symlink():
            raise ValueError('Symbolic links are not supported in backups')
        if path.is_file():
            result[path.relative_to(root).as_posix()] = digest(path)
        elif not path.is_dir():
            raise ValueError('Unsupported file type')
    return result


def database_info(root):
    path = root / 'App_Data' / 'smarttour.db'
    if not path.is_file():
        raise ValueError('Missing smarttour.db; this command supports SQLite backups')
    with closing(sqlite3.connect(path.resolve().as_uri() + '?mode=ro&immutable=1', uri=True)) as db:
        if db.execute('PRAGMA integrity_check').fetchall() != [('ok',)]:
            raise ValueError('SQLite integrity check failed')
        tables = [row[0] for row in db.execute("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name")]
        return {name: db.execute('SELECT count(*) FROM "' + name.replace('"', '""') + '"').fetchone()[0] for name in tables}


def verify(root):
    root = Path(root)
    if root.is_symlink():
        raise ValueError('Backup root cannot be a symbolic link')
    manifest = json.loads((root / 'manifest.json').read_text())
    if manifest.get('version') != 1:
        raise ValueError('Unsupported backup version')
    actual = files(root)
    actual.pop('manifest.json')
    if actual != manifest['files']:
        raise ValueError('Backup file checksum/inventory mismatch')
    if database_info(root) != manifest['tables']:
        raise ValueError('Backup table counts mismatch')
    return manifest


def new_destination(destination, sources):
    destination = Path(destination).absolute()
    if destination.exists() or destination.is_symlink():
        raise ValueError('Destination already exists; refusing overwrite')
    for source in sources:
        source = Path(source).resolve()
        if destination.resolve().is_relative_to(source):
            raise ValueError('Destination must be outside source directories')
    destination.parent.mkdir(parents=True, exist_ok=True)
    return destination


def backup(data, destination, keys=None, config=None):
    data = Path(data)
    if not data.is_dir() or data.is_symlink():
        raise ValueError('App_Data must be a real directory')
    sources = [data] + ([Path(keys)] if keys else [])
    # Validate links before copying: never follow a link to private external files.
    for source in sources:
        if source.is_symlink() or not source.is_dir():
            raise ValueError('Backup sources must be real directories')
        files(source)
    if config and (Path(config).is_symlink() or not Path(config).is_file()):
        raise ValueError('Configuration must be a regular file')
    destination = new_destination(destination, sources)
    with tempfile.TemporaryDirectory(prefix='.smarttour-backup-', dir=destination.parent) as temporary:
        staging = Path(temporary) / 'snapshot'
        staging.mkdir(mode=0o700)
        shutil.copytree(data, staging / 'App_Data')
        # Fold committed WAL into a standalone database in the private copy.
        copied_db = staging / 'App_Data' / 'smarttour.db'
        if not copied_db.is_file():
            raise ValueError('Missing smarttour.db')
        normalized = staging / 'normalized.db'
        with closing(sqlite3.connect(copied_db.resolve().as_uri() + '?mode=ro', uri=True)) as src:
            with closing(sqlite3.connect(normalized)) as dst:
                src.backup(dst)
        for suffix in ('-wal', '-shm'):
            Path(str(copied_db) + suffix).unlink(missing_ok=True)
        normalized.replace(copied_db)
        if keys:
            shutil.copytree(keys, staging / 'DataProtection-Keys')
        if config:
            shutil.copyfile(config, staging / '.env.docker')
            (staging / '.env.docker').chmod(0o600)
        manifest = {'version': 1, 'files': files(staging), 'tables': database_info(staging)}
        (staging / 'manifest.json').write_text(json.dumps(manifest, indent=2) + '\n')
        verify(staging)
        staging.rename(destination)
    return manifest


def restore(source, destination):
    source = Path(source)
    manifest = verify(source)
    destination = new_destination(destination, [source])
    with tempfile.TemporaryDirectory(prefix='.smarttour-restore-', dir=destination.parent) as temporary:
        staging = Path(temporary) / 'snapshot'
        shutil.copytree(source, staging)
        staging.chmod(0o700)
        # Verify the copied bytes too, before making the restored directory visible.
        verify(staging)
        staging.rename(destination)
    return manifest


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    sub = parser.add_subparsers(dest='command', required=True)
    b = sub.add_parser('backup')
    b.add_argument('--data', required=True)
    b.add_argument('--destination', required=True)
    b.add_argument('--keys')
    b.add_argument('--config')
    b.add_argument('--api-stopped', action='store_true', required=True,
                   help='Confirm all processes writing App_Data and keys have stopped')
    for command in ('verify', 'restore'):
        p = sub.add_parser(command)
        p.add_argument('--source', required=True)
        if command == 'restore':
            p.add_argument('--destination', required=True)
    args = parser.parse_args()
    try:
        if args.command == 'backup':
            result = backup(args.data, args.destination, args.keys, args.config)
        elif args.command == 'verify':
            result = verify(args.source)
        else:
            result = restore(args.source, args.destination)
    except (ValueError, OSError, sqlite3.DatabaseError, KeyError, TypeError) as error:
        parser.exit(1, f'FAILED: {error}\n')
    print(f'PASS: {args.command}; {len(result["files"])} verified files; {len(result["tables"])} SQL tables. Keep backup private.')


if __name__ == '__main__':
    main()
