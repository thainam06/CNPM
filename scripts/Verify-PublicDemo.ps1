param([string]$BaseUrl = 'http://localhost:8080')
$ErrorActionPreference = 'Stop'
Set-Location (Split-Path $PSScriptRoot -Parent)
$config = @{}
Get-Content .env.docker | ForEach-Object { if ($_ -match '^([^#=]+)=(.*)$') { $config[$matches[1]] = $matches[2].Trim('"') } }
$login = Invoke-RestMethod "$BaseUrl/api/auth/token" -Method Post -ContentType 'application/json' -Body (@{ username = $config.SMARTTOUR_ADMIN_USERNAME; password = $config.SMARTTOUR_ADMIN_PASSWORD } | ConvertTo-Json)
$headers = @{ Authorization = "Bearer $($login.accessToken)" }
$devices = 1..12 | ForEach-Object { [guid]::NewGuid().ToString() }
$http = [System.Net.Http.HttpClient]::new()
try {
    $tasks = foreach ($device in $devices) {
        $body = @{ consent = $true; deviceId = $device; device = @{ browser = 'Chrome'; operatingSystem = 'Windows'; type = 'Desktop' }; events = @(@{ id = [guid]::NewGuid().ToString(); kind = 'heartbeat'; placeId = $null; language = 'vi'; occurredAt = [DateTimeOffset]::UtcNow.ToString('O') }) } | ConvertTo-Json -Depth 5
        $content = [System.Net.Http.StringContent]::new($body, [Text.Encoding]::UTF8, 'application/json')
        $http.PostAsync("$BaseUrl/api/analytics/batch", $content)
    }
    foreach ($task in $tasks) { $response = $task.GetAwaiter().GetResult(); $response.EnsureSuccessStatusCode() | Out-Null; $response.Dispose() }
    $found = 0
    for ($i = 0; $i -lt 10; $i++) {
        $snapshot = Invoke-RestMethod "$BaseUrl/api/admin/devices?pageSize=100" -Headers $headers
        $found = @($snapshot.items | Where-Object { $_.deviceId -in $devices -and $_.online }).Count
        if ($found -eq 12) { break }
        Start-Sleep -Seconds 1
    }
    if ($found -ne 12) { throw "Expected 12 test devices; found $found." }
    if (@($snapshot.items | Where-Object { $_.deviceId -in $devices -and $_.descriptor.browser -eq 'Chrome' -and $_.descriptor.operatingSystem -eq 'Windows' }).Count -ne 12) { throw 'Device descriptions were not persisted.' }
    $places = Invoke-RestMethod "$BaseUrl/api/places"
    Write-Output "PASS: $($places.Count) POIs; 12 concurrent simulated devices recorded in $($snapshot.storage)."
} finally {
    foreach ($device in $devices) {
        Invoke-RestMethod "$BaseUrl/api/analytics/forget" -Method Post -ContentType 'application/json' -Body (@{ deviceId = $device } | ConvertTo-Json) | Out-Null
    }
    $http.Dispose()
}
$after = Invoke-RestMethod "$BaseUrl/api/admin/devices?pageSize=100" -Headers $headers
if (@($after.items | Where-Object { $_.deviceId -in $devices }).Count) { throw 'Test devices were not removed.' }
Write-Output 'PASS: all synthetic device records removed through consent-revocation API.'
