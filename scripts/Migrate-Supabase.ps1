param([string]$EnvFile, [string]$CertificateFile, [switch]$Apply,
      [switch]$BackupConfirmed, [switch]$AcceptCompactSnapshot)
$ErrorActionPreference = 'Stop'
if ($Apply -and !$BackupConfirmed) {
    throw 'Apply requires a verified external backup and stopped writers. Run the plan first; then use -BackupConfirmed only when those prerequisites are satisfied.'
}
$taskRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Docker-Context.ps1')
$context = Get-SmartTourDockerContext -Root $taskRoot -EnvFile $EnvFile -Supabase
$certificate = Resolve-SmartTourCertificate -Root $taskRoot -CertificateFile $CertificateFile
$taskDocker = Get-Command docker -ErrorAction Stop
Push-Location $taskRoot
try {
    $compose = $context.Arguments
    $engine = & $taskDocker.Source info --format '{{.OSType}}' 2>$null
    if ($LASTEXITCODE -ne 0 -or $engine -ne 'linux') { throw 'Start Docker Desktop with Linux containers before migration planning.' }
    $validation = @(& $taskDocker.Source @compose config --quiet 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'Private Compose/env configuration is invalid. Check it locally; do not share secret values.' }
    Write-Host 'Building the standalone migration tool; web and workers will not be started.'
    & $taskDocker.Source @compose build api
    if ($LASTEXITCODE -ne 0) { throw 'Build failed before migration. Cloud data was not changed.' }
    $run = @('run','--rm','-T','--no-deps')
    if ($certificate) { $run += @('-v',"${certificate}:/app/App_Data/supabase-ca.crt:ro") }
    $arguments = @('--MigratePostgresSixTables=true','--ImportPostgres=false','--CheckPostgres=false','--ReviewPostgres=false',
        "--ApplyPostgresMigration=$($Apply.IsPresent.ToString().ToLowerInvariant())",
        "--BackupConfirmed=$($BackupConfirmed.IsPresent.ToString().ToLowerInvariant())",
        "--AcceptCompactSnapshot=$($AcceptCompactSnapshot.IsPresent.ToString().ToLowerInvariant())")
    if ($certificate) { $arguments += '--Storage:PostgresRootCertificate=/app/App_Data/supabase-ca.crt' }
    if ($Apply) { Write-Host 'APPLY requested: original 15 tables will be archived only after all guards pass.' }
    else { Write-Host 'PLAN ONLY: SELECT inspection, no schema or data changes.' }
    & $taskDocker.Source @compose @run api @arguments
    if ($LASTEXITCODE -ne 0) { throw 'Migration blocked or failed. Read the safe diagnostic code/blockers; do not delete tables, rerun import or reset passwords.' }
    Write-Host 'Read the JSON code and initialized flag. This command does not start or certify the web application.'
} finally { Pop-Location }
