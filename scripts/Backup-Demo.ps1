$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe' }
$arguments = @('compose','--env-file','.env.docker','-f','compose.yaml')
$container = & $docker @arguments ps -q api
if ($LASTEXITCODE -or !$container) { throw 'Start the API before creating a backup.' }
$destination = Join-Path (Get-Location) ('artifacts/backups/' + (Get-Date -Format 'yyyyMMdd-HHmmss'))
New-Item -ItemType Directory -Path $destination -Force | Out-Null
try {
    # Stop cleanly so SQLite WAL, local media and encryption keys form one consistent backup.
    & $docker @arguments stop api
    if ($LASTEXITCODE) { throw 'Could not stop API for consistent backup.' }
    & $docker cp "${container}:/app/App_Data" (Join-Path $destination 'App_Data')
    if ($LASTEXITCODE) { throw 'Data backup failed.' }
    & $docker cp "${container}:/home/app/.aspnet/DataProtection-Keys" (Join-Path $destination 'DataProtection-Keys')
    if ($LASTEXITCODE) { throw 'Key backup failed.' }
    Copy-Item -LiteralPath .env.docker -Destination (Join-Path $destination '.env.docker')
    Write-Host "Backup created: $destination. Contains private credentials and data; keep it private."
} finally {
    & $docker @arguments start api
    if ($LASTEXITCODE) { throw 'Restart API manually with docker compose --env-file .env.docker up -d --wait.' }
}
