param([switch]$Force)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$workspace = Split-Path -Parent $PSScriptRoot
$toolsFolder = Join-Path $workspace 'artifacts/map-tools'
$mapFolder = Join-Path $workspace 'ShortVideoTranslator.Api/App_Data/maps'
$archivePath = Join-Path $mapFolder 'district1.pmtiles'
if ((Test-Path -LiteralPath $archivePath) -and !$Force) {
    throw 'Gói đã tồn tại. Dùng -Force khi chủ động muốn tạo lại gói Quận 1.'
}
New-Item -ItemType Directory -Path $toolsFolder,$mapFolder -Force | Out-Null
$zipPath = Join-Path $toolsFolder 'pmtiles.zip'
$expectedToolHash = 'a658baa4d7e55020aef6ca17bd9ff9faa1582671266b36f58c52db0ac8e785a1'
if (!(Test-Path -LiteralPath $zipPath)) {
    Invoke-WebRequest 'https://github.com/protomaps/go-pmtiles/releases/download/v1.31.2/go-pmtiles_1.31.2_Windows_x86_64.zip' -OutFile $zipPath
}
if ((Get-FileHash -LiteralPath $zipPath -Algorithm SHA256).Hash.ToLowerInvariant() -ne $expectedToolHash) { throw 'Checksum công cụ không đúng; không thực thi.' }
Expand-Archive -LiteralPath $zipPath -DestinationPath $toolsFolder -Force
$cliPath = Join-Path $toolsFolder 'pmtiles.exe'
$tempArchive = Join-Path $mapFolder (([guid]::NewGuid().ToString('N')) + '.pmtiles')
$sourceUrl = 'https://build.protomaps.com/20261013.pmtiles'
& $cliPath extract $sourceUrl $tempArchive '--bbox=106.6853694,10.7625222,106.7100000,10.7900000' '--maxzoom=15' '--download-threads=2'
if ($LASTEXITCODE -ne 0) { throw 'Không trích xuất được. Gói cũ chưa bị thay đổi.' }
& $cliPath verify $tempArchive
if ($LASTEXITCODE -ne 0) { throw 'Gói không qua verify. Gói cũ chưa bị thay đổi.' }
$descriptor = @{
    version = 'protomaps-20261013-q1-v1'; sourceUrl = $sourceUrl
    license = 'ODbL Produced Work; OpenStreetMap attribution required'
    attribution = 'OpenStreetMap contributors / Protomaps / Natural Earth'
    bounds = @(106.6853694,10.7625222,106.7100000,10.7900000)
    sha256 = (Get-FileHash -LiteralPath $tempArchive -Algorithm SHA256).Hash.ToLowerInvariant()
    bytes = (Get-Item -LiteralPath $tempArchive).Length; minZoom = 0; maxZoom = 15
}
$tempMetadata = Join-Path $mapFolder 'pack.json.tmp'
$descriptor | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath $tempMetadata -Encoding UTF8
Move-Item -LiteralPath $tempArchive -Destination $archivePath -Force
Move-Item -LiteralPath $tempMetadata -Destination (Join-Path $mapFolder 'pack.json') -Force
Write-Output 'Gói Quận 1 đã tạo và xác minh. File lớn/công cụ nằm trong Git ignore.'
