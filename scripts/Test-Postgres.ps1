# Isolated integration test. Does not connect to Supabase or demo volumes.
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskName = 'smarttour-pg-test-' + [guid]::NewGuid().ToString('N').Substring(0,8)
$taskPassword = [guid]::NewGuid().ToString('N')
$taskPrevious = $env:SMARTTOUR_TEST_POSTGRES
$taskStarted = $false
Push-Location $taskRoot
try {
    docker run --rm -d --name $taskName -p '127.0.0.1::5432' -e "POSTGRES_PASSWORD=$taskPassword" -e POSTGRES_DB=smarttour_tests postgres:17-alpine | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'Cannot start isolated PostgreSQL.' }
    $taskStarted = $true
    $taskReady = $false
    for ($i=0; $i -lt 40; $i++) {
        # pg_isready may succeed on the temporary bootstrap server before the
        # requested database exists. TCP opens after the entrypoint finishes.
        docker exec -e "PGPASSWORD=$taskPassword" $taskName psql -w -h 127.0.0.1 -U postgres -d smarttour_tests -c 'SELECT 1' *> $null
        if ($LASTEXITCODE -eq 0) { $taskReady=$true; break }
        Start-Sleep -Milliseconds 500
    }
    if(-not $taskReady){throw 'Isolated PostgreSQL database did not become ready.'}
    foreach ($taskFile in @('002_smarttour_compact.sql','005_six_table_backend.sql')) {
        $taskResult = Get-Content (Join-Path $taskRoot "database/postgres/$taskFile") -Raw | docker exec -i $taskName psql -X -v ON_ERROR_STOP=1 -U postgres -d smarttour_tests 2>&1
        if ($LASTEXITCODE -ne 0) { throw ($taskResult | Out-String) }
    }
    $taskPortText = docker port $taskName 5432
    $taskPort = ($taskPortText.Trim() -split ':')[-1]
    $env:SMARTTOUR_TEST_POSTGRES = "Host=127.0.0.1;Port=$taskPort;Database=smarttour_tests;Username=postgres;Password=$taskPassword;SSL Mode=Disable"
    dotnet test --filter 'FullyQualifiedName~Postgres' --verbosity minimal
    if ($LASTEXITCODE -ne 0) { throw 'PostgreSQL integration tests failed.' }
} finally {
    $env:SMARTTOUR_TEST_POSTGRES = $taskPrevious
    if ($taskStarted) { docker stop $taskName | Out-Null }
    Pop-Location
}
