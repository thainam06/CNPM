# Run interactively; password input is hidden and never printed.
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskFolder=Join-Path $taskRoot 'ShortVideoTranslator.Api/App_Data'
$taskPath=Join-Path $taskFolder 'postgres.local.json'
$taskHost=(Read-Host 'Host trong Connect > Session pooler').Trim()
if($taskHost -notmatch '^[a-zA-Z0-9.-]+$'){throw 'Host khong hop le.'}
$taskPortText=Read-Host 'Port [5432]'
$taskPort=if([string]::IsNullOrWhiteSpace($taskPortText)){5432}else{[int]$taskPortText}
if($taskPort -lt 1 -or $taskPort -gt 65535){throw 'Port khong hop le.'}
$taskDatabase=Read-Host 'Database [postgres]'
if([string]::IsNullOrWhiteSpace($taskDatabase)){$taskDatabase='postgres'}
$taskUser=Read-Host 'User (giu nguyen ten hien tren Supabase)'
if([string]::IsNullOrWhiteSpace($taskUser)){throw 'Thieu User.'}
$taskPassword=Read-Host 'Mat khau DATABASE (khong phai mat khau Admin web)' -AsSecureString
$taskPointer=[Runtime.InteropServices.Marshal]::SecureStringToBSTR($taskPassword)
try {
    $taskBuilder=New-Object System.Data.Common.DbConnectionStringBuilder
    $taskBuilder['Host']=$taskHost; $taskBuilder['Port']=$taskPort
    $taskBuilder['Database']=$taskDatabase; $taskBuilder['Username']=$taskUser
    $taskBuilder['Password']=[Runtime.InteropServices.Marshal]::PtrToStringBSTR($taskPointer)
    $taskBuilder['SSL Mode']='VerifyFull'
    if(Test-Path -LiteralPath $taskPath){
        $taskConfirm=Read-Host 'Da co cau hinh. Nhap THAY THE de ghi de cau hinh (khong doi database)'
        if($taskConfirm -cne 'THAY THE'){throw 'Da giu nguyen cau hinh cu.'}
    }
    New-Item -ItemType Directory -Force -Path $taskFolder | Out-Null
    # DbConnectionStringBuilder implements IDictionary; use the explicit accessor
    # so PowerShell reads the serialized property rather than a dictionary key.
    @{connectionString=$taskBuilder.get_ConnectionString()} | ConvertTo-Json | Set-Content -LiteralPath $taskPath -Encoding utf8
    Write-Output 'Da luu cau hinh local trong App_Data bi Git ignore. Khong gui file nay vao chat/Git.'
} finally {
    [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($taskPointer)
    if($taskBuilder){$taskBuilder.Clear()}
    $taskPassword.Dispose()
}
