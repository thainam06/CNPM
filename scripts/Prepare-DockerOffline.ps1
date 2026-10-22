param([string]$BaseUrl = 'http://localhost:8080')
$ErrorActionPreference = 'Stop'
$taskRoot = Split-Path $PSScriptRoot -Parent
$taskDocker = (Get-Command docker -ErrorAction Stop).Source
Push-Location $taskRoot
try {
    $taskContainer = & $taskDocker compose --env-file .env.docker ps -q api
    if (!$taskContainer) { throw 'Start the Docker API before preparing offline assets.' }
    $taskPlaces = Invoke-RestMethod "$BaseUrl/api/places"
    $taskStage = Join-Path $taskRoot ('artifacts/offline-seed/' + [guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path "$taskStage/audio","$taskStage/maps" -Force | Out-Null
    $taskMap = Get-Content 'ShortVideoTranslator.Api/App_Data/maps/pack.json' -Raw | ConvertFrom-Json
    $taskMapPath = 'ShortVideoTranslator.Api/App_Data/maps/district1.pmtiles'
    if ((Get-FileHash $taskMapPath).Hash.ToLowerInvariant() -ne $taskMap.sha256) { throw 'Map checksum mismatch.' }
    Copy-Item -LiteralPath $taskMapPath -Destination "$taskStage/maps/district1.pmtiles"
    Copy-Item -LiteralPath 'ShortVideoTranslator.Api/App_Data/maps/pack.json' -Destination "$taskStage/maps/pack.json"
    $taskCount = 0
    foreach ($taskPlace in $taskPlaces) {
        foreach ($taskLanguage in @('vi','en','zh','ja','ko','fr')) {
            $taskRevision = $taskPlace.narrationRevisions.$taskLanguage
            $taskInput = if ($taskRevision) { "v2:${taskLanguage}:${taskRevision}:$($taskPlace.description)" } else { "v1:${taskLanguage}:$($taskPlace.description)" }
            $taskSha = [Security.Cryptography.SHA256]::Create()
            try { $taskKey = ([BitConverter]::ToString($taskSha.ComputeHash([Text.Encoding]::UTF8.GetBytes($taskInput)))).Replace('-','').ToLowerInvariant() } finally { $taskSha.Dispose() }
            $taskAudio = "ShortVideoTranslator.Api/App_Data/audio/$taskKey"
            $taskMeta = Get-Content "$taskAudio.json" -Raw | ConvertFrom-Json
            if ($taskMeta.Key -ne $taskKey -or $taskMeta.Language -ne $taskLanguage -or (Get-Item "$taskAudio.mp3").Length -ne $taskMeta.Bytes -or (Get-FileHash "$taskAudio.mp3").Hash.ToLowerInvariant() -ne $taskMeta.Sha256) { throw "Invalid audio: $($taskPlace.name) / $taskLanguage" }
            Copy-Item -LiteralPath "$taskAudio.json","$taskAudio.mp3" -Destination "$taskStage/audio"
            $taskCount++
        }
    }
    # Copy only validated public assets. Never copy accounts, secrets or platform.json.
    & $taskDocker exec $taskContainer mkdir -p /app/App_Data/audio /app/App_Data/maps
    if ($LASTEXITCODE) { throw 'Asset folder creation failed.' }
    & $taskDocker cp "$taskStage/audio/." "${taskContainer}:/app/App_Data/audio/"
    if ($LASTEXITCODE) { throw 'Audio import failed.' }
    & $taskDocker exec $taskContainer mkdir -p /app/App_Data/maps
    if ($LASTEXITCODE) { throw 'Map folder creation failed.' }
    & $taskDocker cp "$taskStage/maps/." "${taskContainer}:/app/App_Data/maps/"
    if ($LASTEXITCODE) { throw 'Map import failed.' }
    & $taskDocker exec -u root $taskContainer chown -R app:app /app/App_Data/audio /app/App_Data/maps
    if ($LASTEXITCODE) { throw 'Asset ownership update failed.' }
    foreach ($taskLanguage in @('vi','en','zh','ja','ko','fr')) {
        $taskManifest = Invoke-RestMethod "$BaseUrl/api/narration/pack-manifest?language=$taskLanguage"
        if ($taskManifest.totalFiles -ne $taskPlaces.Count) { throw "Incomplete manifest: $taskLanguage" }
    }
    $null = Invoke-RestMethod "$BaseUrl/api/maps/pack-manifest"
    Write-Output "Verified $taskCount narration assets and District 1 map in Docker. Download an offline bundle in each visitor browser before disconnecting."
} finally { Pop-Location }
