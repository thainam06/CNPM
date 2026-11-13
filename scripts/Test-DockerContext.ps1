$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'Docker-Context.ps1')
$folder = Join-Path ([IO.Path]::GetTempPath()) ('smarttour-context-' + [guid]::NewGuid().ToString('N'))
$previous = $env:SUPABASE_CONNECTION_STRING
function Assert-True($value, $message) { if (!$value) { throw $message } }
try {
    $env:SUPABASE_CONNECTION_STRING = $null
    New-Item -ItemType Directory -Path (Join-Path $folder 'scripts') -Force | Out-Null
    $nested = Join-Path $folder 'scripts/.env.docker'
    Set-Content $nested "SMARTTOUR_PORT=8081`n# SUPABASE_CONNECTION_STRING=commented"
    $ctx = Get-SmartTourDockerContext -Root $folder
    Assert-True (!$ctx.Supabase) 'SQLite-only configuration must stay SQLite.'
    Assert-True ($ctx.EnvFile -eq $nested) 'Legacy scripts env must be discovered.'
    Add-Content $nested "SUPABASE_CONNECTION_STRING='secret-test-value'"
    $ctx = Get-SmartTourDockerContext -Root $folder
    Assert-True $ctx.Supabase 'Cloud setting must select Supabase.'
    Assert-True ($ctx.Arguments[-1] -eq (Join-Path $folder 'compose.supabase.yaml')) 'Missing cloud override.'
    Assert-True (($ctx.Arguments -join ' ') -notmatch 'secret-test-value') 'Secret must not be in command arguments.'
    Set-Content (Join-Path $folder '.env.docker') "SMARTTOUR_PORT=8082"
    Assert-True (!(Get-SmartTourDockerContext -Root $folder).Supabase) 'Root env must take precedence.'
    Assert-True (Get-SmartTourDockerContext -Root $folder -EnvFile 'scripts/.env.docker').Supabase 'Explicit env must be honored.'
    Assert-True (Get-SmartTourDockerContext -Root $folder -Supabase).Supabase 'Explicit Supabase must be honored.'
    $env:SUPABASE_CONNECTION_STRING = 'environment-test-value'
    Assert-True (Get-SmartTourDockerContext -Root $folder).Supabase 'Environment binding must select Supabase.'
    # Parse Windows scripts without executing Docker commands.
    foreach ($name in @('Docker-Demo.ps1','Start-PublicDemo.ps1','Start-Supabase.ps1')) {
        $tokens = $null; $errors = $null
        [System.Management.Automation.Language.Parser]::ParseFile((Join-Path $PSScriptRoot $name), [ref]$tokens, [ref]$errors) | Out-Null
        Assert-True ($errors.Count -eq 0) "PowerShell syntax error in $name"
        $source = Get-Content (Join-Path $PSScriptRoot $name) -Raw
        Assert-True ($source.Contains('Get-SmartTourDockerContext')) "Shared provider selection missing in $name"
    }
    Write-Host 'PASS: Docker provider/env selection and PowerShell script syntax.'
} finally {
    $env:SUPABASE_CONNECTION_STRING = $previous
    if (Test-Path $folder) { Remove-Item $folder -Recurse -Force }
}
