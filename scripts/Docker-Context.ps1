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

function Resolve-SmartTourCertificate {
    param([Parameter(Mandatory=$true)][string]$Root, [string]$CertificateFile)
    if ($CertificateFile) {
        if (![IO.Path]::IsPathRooted($CertificateFile)) { $CertificateFile = Join-Path $Root $CertificateFile }
        if (!(Test-Path -LiteralPath $CertificateFile -PathType Leaf)) { throw 'The specified CA certificate file does not exist. Keep TLS verification enabled.' }
        return (Resolve-Path -LiteralPath $CertificateFile).ProviderPath
    }
    foreach ($name in @('supabase-ca.crt','supabase-ca.crt.crt')) {
        $candidate = Join-Path $Root $name
        if (Test-Path -LiteralPath $candidate -PathType Leaf) { return (Resolve-Path -LiteralPath $candidate).ProviderPath }
    }
    return $null
}
