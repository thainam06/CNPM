param([ValidateSet('Start','Stop','Status','Logs')][string]$Action = 'Start', [switch]$Named, [switch]$Build, [string]$EnvFile, [switch]$Supabase)
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
Set-Location $taskRoot
. (Join-Path $PSScriptRoot 'Docker-Context.ps1')
$context = Get-SmartTourDockerContext -Root $taskRoot -EnvFile $EnvFile -Supabase:$Supabase
$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe' }
& $docker info --format '{{.ServerVersion}}'
if ($LASTEXITCODE) { throw 'Start Docker Desktop and wait for Engine running.' }
$override = if ($Named) { 'compose.tunnel.named.yaml' } else { 'compose.tunnel.yaml' }
$arguments = $context.Arguments + @('-f',(Join-Path $taskRoot $override))
if ($context.Supabase) { Write-Host 'Database mode: Supabase PostgreSQL' }
switch ($Action) {
    'Start' {
        $startArguments = @('up','-d','--wait','--wait-timeout','180')
        if ($Build) { $startArguments += '--build' }
        & $docker @arguments @startArguments
        if ($LASTEXITCODE) { throw 'Docker startup failed. Check docker compose logs.' }
        if ($Named) { Write-Host 'Open the hostname configured in your Cloudflare tunnel dashboard.' }
        else { Write-Host 'Use the https://...trycloudflare.com URL below. It changes when the tunnel is recreated.' }
        & $docker @arguments logs --tail 50 tunnel
    }
    'Stop' { & $docker @arguments stop }
    'Status' { & $docker @arguments ps }
    'Logs' { & $docker @arguments logs --tail 60 }
}
if ($LASTEXITCODE) { throw 'Docker command failed.' }
