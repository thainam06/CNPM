param([ValidateSet('Init','Start','Stop','Status','Logs','Check')][string]$Action = 'Start')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskEnv = Join-Path $taskRoot '.env.docker'
if ($Action -eq 'Init') {
    if (Test-Path -LiteralPath $taskEnv) { Write-Host '.env.docker already exists. It was kept unchanged.'; exit 0 }
    $taskRng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
    try {
        $taskBytes = New-Object byte[] 48; $taskRng.GetBytes($taskBytes)
        $taskSecret = [Convert]::ToBase64String($taskBytes)
        $taskRng.GetBytes($taskBytes)
        $taskPassword = [Convert]::ToBase64String($taskBytes).Substring(0, 18)
    } finally { $taskRng.Dispose() }
    [System.IO.File]::WriteAllText($taskEnv, "SMARTTOUR_PORT=8080`nSMARTTOUR_ADMIN_USERNAME=admin`nSMARTTOUR_ADMIN_PASSWORD=$taskPassword`nSMARTTOUR_JWT_SECRET=$taskSecret`n")
    Write-Host 'Created .env.docker (ignored by Git). Open this local file to see your Docker-only admin credentials.'
    Write-Host 'The existing local API account and App_Data were not changed.'
    exit 0
}
$taskDocker = Get-Command docker -ErrorAction SilentlyContinue
if (!$taskDocker) { throw 'Docker CLI is unavailable. Open Docker Desktop and then open a new terminal.' }
if (!(Test-Path -LiteralPath $taskEnv)) { throw 'Run: powershell -File scripts/Docker-Demo.ps1 -Action Init' }
Push-Location $taskRoot
try {
    $taskArguments = @('compose','--env-file',$taskEnv,'-f','compose.yaml')
    if ($Action -eq 'Check') { & $taskDocker.Source @taskArguments config --quiet; if ($LASTEXITCODE -ne 0) { throw 'Compose validation failed.' }; exit 0 }
    & $taskDocker.Source info --format '{{.OSType}}'
    if ($LASTEXITCODE -ne 0) { throw 'Docker Engine is not ready. Open Docker Desktop, select Linux containers and wait for Engine running.' }
    switch ($Action) {
        'Start' { & $taskDocker.Source @taskArguments up --build -d --wait --wait-timeout 240; if ($LASTEXITCODE -eq 0) { Write-Host 'Open http://localhost:8080 (or SMARTTOUR_PORT from .env.docker). Admin: /admin.' } }
        'Stop' { & $taskDocker.Source @taskArguments down }
        'Status' { & $taskDocker.Source @taskArguments ps }
        'Logs' { & $taskDocker.Source @taskArguments logs --tail 100 }
    }
    if ($LASTEXITCODE -ne 0) { throw 'Docker command failed. See the output above.' }
} finally { Pop-Location }
