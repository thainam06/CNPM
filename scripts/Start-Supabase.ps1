param([string]$EnvFile, [switch]$CheckOnly, [switch]$ReviewOnly, [switch]$IncludeConnectionInfo)
$ErrorActionPreference = 'Stop'
if ($CheckOnly -and $ReviewOnly) { throw 'Choose -CheckOnly or -ReviewOnly, not both.' }
if ($IncludeConnectionInfo -and !$ReviewOnly) { throw '-IncludeConnectionInfo requires -ReviewOnly.' }
$taskRoot = Split-Path $PSScriptRoot -Parent
. (Join-Path $PSScriptRoot 'Docker-Context.ps1')
$context = Get-SmartTourDockerContext -Root $taskRoot -EnvFile $EnvFile -Supabase
$taskDocker = Get-Command docker -ErrorAction SilentlyContinue
if (!$taskDocker) { throw 'Open Docker Desktop, wait for Engine running, and open a new terminal.' }
Push-Location $taskRoot
try {
    $compose = $context.Arguments
    $engine = & $taskDocker.Source info --format '{{.OSType}}' 2>$null
    if ($LASTEXITCODE -ne 0 -or $engine -ne 'linux') { throw 'Start Docker Desktop and select Linux containers.' }
    # Do not echo parse errors: malformed dotenv lines can contain a secret.
    $validation = @(& $taskDocker.Source @compose config --quiet 2>&1)
    if ($LASTEXITCODE -ne 0) { throw 'Compose/env configuration is invalid. Check .env.docker locally; each setting must be a complete KEY=value line.' }
    Write-Host 'Building the current API diagnostic (never imports or resets cloud data)...'
    & $taskDocker.Source @compose build api
    if ($LASTEXITCODE -ne 0) { throw 'API build failed. Database data was not changed.' }
    if (!$CheckOnly -and !$ReviewOnly) {
        $certificate = Join-Path $taskRoot 'supabase-ca.crt'
        if (!(Test-Path -LiteralPath $certificate)) { $certificate = Join-Path $taskRoot 'supabase-ca.crt.crt' }
        if (Test-Path -LiteralPath $certificate) {
            & $taskDocker.Source @compose run --rm -T --no-deps --user 0 --entrypoint sh -v "$($certificate):/tmp/smarttour-ca.crt:ro" api -c 'cp /tmp/smarttour-ca.crt /app/App_Data/supabase-ca.crt && chmod 644 /app/App_Data/supabase-ca.crt'
            if ($LASTEXITCODE -ne 0) { throw 'Could not copy the Supabase CA into the existing data volume.' }
        }
    }
    Write-Host 'Checking Supabase directly, before starting web/worker processes...'
    if ($ReviewOnly) {
        $reviewRun = @('run','--rm','-T','--no-deps')
        $reviewCertificate = Join-Path $taskRoot 'supabase-ca.crt'
        if (!(Test-Path -LiteralPath $reviewCertificate)) { $reviewCertificate = Join-Path $taskRoot 'supabase-ca.crt.crt' }
        if (Test-Path -LiteralPath $reviewCertificate) {
            # Make the existing public CA available without copying into the data volume.
            $reviewRun += @('-v',"${reviewCertificate}:/app/App_Data/supabase-ca.crt:ro")
        }
        $reviewArguments = @('--ReviewPostgres=true','--CheckPostgres=false','--ImportPostgres=false')
        if ($IncludeConnectionInfo) { $reviewArguments += '--IncludeConnectionInfo=true' }
        & $taskDocker.Source @compose @reviewRun api @reviewArguments
    } else {
        & $taskDocker.Source @compose run --rm -T --no-deps api --CheckPostgres=true --ImportPostgres=false
    }
    if ($LASTEXITCODE -ne 0) { throw 'Supabase check failed. Send the diagnostic code above (TLS/AUTH/CONNECT/SCHEMA/DATA). Do not delete tables or rerun import.' }
    if (!$CheckOnly -and !$ReviewOnly) {
        & $taskDocker.Source @compose build web
        if ($LASTEXITCODE -ne 0) { throw 'Web build failed; Supabase check passed.' }
        # Recreate web too so nginx resolves the current API container address.
        & $taskDocker.Source @compose up -d --force-recreate --wait --wait-timeout 240 api web
        if ($LASTEXITCODE -ne 0) { throw 'Database check passed but application startup failed. Check container health/logs; no import was run.' }
        $published = @(& $taskDocker.Source @compose port web 80)
        if ($LASTEXITCODE -ne 0 -or !$published.Count) { throw 'Cannot find the published web port.' }
        $taskPort = ($published[0].Trim() -split ':')[-1]
        if ($taskPort -notmatch '^\d+$') { throw 'Unexpected published port. Check docker compose ps.' }
        $url = "http://localhost:$taskPort"
        try {
            $health = Invoke-RestMethod "$url/api/health" -TimeoutSec 30
            $ready = Invoke-RestMethod "$url/api/health/ready" -TimeoutSec 30
            $page = Invoke-WebRequest "$url/" -UseBasicParsing -TimeoutSec 30
        } catch { throw "The local web/API check failed (possibly HTTP 404/502/503). Open $url and share the URL/status, not .env contents." }
        if ($health.storage -ne 'postgresql' -or $ready.status -ne 'ready' -or $page.StatusCode -ne 200) {
            throw 'Web is not verified as ready with PostgreSQL. Do not use the old tunnel URL yet.'
        }
        Write-Host "PASS: web + PostgreSQL are ready. Open $url (admin: $url/admin)."
        Write-Host 'For phone access, open a fresh Cloudflare tunnel to this URL; an old Quick Tunnel URL may be expired.'
    }
} finally { Pop-Location }
