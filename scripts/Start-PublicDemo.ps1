param([ValidateSet('Start','Stop','Status','Logs')][string]$Action = 'Start', [switch]$Named, [switch]$Build)
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$docker = (Get-Command docker -ErrorAction SilentlyContinue).Source
if (!$docker) { $docker = 'C:\Program Files\Docker\Docker\resources\bin\docker.exe' }
if (!(Test-Path .env.docker)) { throw 'Run scripts/Docker-Demo.ps1 -Action Init before starting.' }
& $docker info --format '{{.ServerVersion}}'
if ($LASTEXITCODE) { throw 'Start Docker Desktop and wait for Engine running.' }
$override = if ($Named) { 'compose.tunnel.named.yaml' } else { 'compose.tunnel.yaml' }
$arguments = @('compose','--env-file','.env.docker','-f','compose.yaml','-f',$override)
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
