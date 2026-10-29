# Consistent SQLite online backup, including committed WAL data; no API downtime.
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskDocker = (Get-Command docker -ErrorAction Stop).Source
$taskStamp = [TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow, 'SE Asia Standard Time').ToString('yyyyMMdd-HHmmss')
$taskDestination = Join-Path $taskRoot "artifacts/backups/sqlite-$taskStamp"
$taskRemote = '/tmp/smarttour-snapshot-' + [guid]::NewGuid().ToString('N') + '.db'
Push-Location $taskRoot
try {
    $taskContainer = & $taskDocker compose --env-file .env.docker -f compose.yaml ps -q api
    if ($LASTEXITCODE -ne 0 -or !$taskContainer) { throw 'API container is not running. Start Docker demo first.' }
    $taskPython = @"
import sqlite3
from pathlib import Path
src_path=Path('/app/App_Data/smarttour.db')
if not src_path.is_file():
    raise RuntimeError('SQLite source not found; no backup created')
with sqlite3.connect('file:/app/App_Data/smarttour.db?mode=ro',uri=True,timeout=30) as src:
    with sqlite3.connect('$taskRemote') as dst:
        src.backup(dst,pages=256,sleep=0.05)
        if dst.execute('PRAGMA integrity_check').fetchone()[0] != 'ok':
            raise RuntimeError('SQLite integrity check failed')
        print('SQLite integrity_check: ok')
        print('Places:',dst.execute('SELECT count(*) FROM places').fetchone()[0])
"@
    & $taskDocker exec $taskContainer python3 -c $taskPython
    if ($LASTEXITCODE -ne 0) { throw 'Online backup failed.' }
    New-Item -ItemType Directory -Path $taskDestination -Force | Out-Null
    $taskFile = Join-Path $taskDestination 'smarttour.db'
    & $taskDocker cp "${taskContainer}:$taskRemote" $taskFile
    if ($LASTEXITCODE -ne 0) { throw 'Copying backup failed.' }
    Write-Output "Backup: $taskFile"
    Write-Output ('SHA256: ' + (Get-FileHash -LiteralPath $taskFile -Algorithm SHA256).Hash)
    Write-Output 'SQLite only. For audio, encryption keys and configuration, also use Backup-Demo.ps1.'
} finally {
    if ($taskContainer) {
        # Exact generated /tmp file only; never delete anything from App_Data.
        & $taskDocker exec $taskContainer python3 -c "from pathlib import Path; Path('$taskRemote').unlink(missing_ok=True)" | Out-Null
    }
    Pop-Location
}
