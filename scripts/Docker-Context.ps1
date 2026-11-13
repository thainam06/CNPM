# Shared selection: a configured cloud database must survive every start/rebuild.
function Get-SmartTourDockerContext {
    param([Parameter(Mandatory=$true)][string]$Root, [string]$EnvFile, [switch]$Supabase)
    if (!$EnvFile) {
        $EnvFile = Join-Path $Root '.env.docker'
        if (!(Test-Path -LiteralPath $EnvFile)) { $EnvFile = Join-Path $Root 'scripts/.env.docker' }
    } elseif (![IO.Path]::IsPathRooted($EnvFile)) { $EnvFile = Join-Path $Root $EnvFile }
    if (!(Test-Path -LiteralPath $EnvFile)) { throw 'Missing .env.docker. Keep your existing credentials; specify -EnvFile if stored elsewhere.' }
    # Only inspect the presence of the setting, never evaluate dotenv as PowerShell.
    $configured = @(Get-Content -LiteralPath $EnvFile | Where-Object { $_ -match '^\s*SUPABASE_CONNECTION_STRING\s*=' }).Count -gt 0
    $cloud = $Supabase -or $configured -or ![string]::IsNullOrWhiteSpace($env:SUPABASE_CONNECTION_STRING)
    $compose = @('compose','--env-file',$EnvFile,'-f',(Join-Path $Root 'compose.yaml'))
    if ($cloud) { $compose += @('-f',(Join-Path $Root 'compose.supabase.yaml')) }
    [pscustomobject]@{ EnvFile=$EnvFile; Supabase=[bool]$cloud; Arguments=$compose }
}
