param(
    [Parameter(Mandatory = $true)]
    [string]$ExePath,
    [string]$ServiceName = "MailSenderService"
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path -LiteralPath $ExePath)) {
    throw "ExePath khong ton tai: $ExePath"
}

$fullExe = (Resolve-Path -LiteralPath $ExePath).Path

Write-Host "Installing service $ServiceName with binary: $fullExe"

# Stop & xoa service cu (neu co)
$existing = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -ne $existing) {
    Write-Host "Service da ton tai -> stop va xoa truoc..."
    & sc.exe stop $ServiceName | Out-Null
    Start-Sleep -Seconds 1
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 1
}

# IMPORTANT: sc.exe yeu cau cu phap dac biet:
#   <option>= <SPACE> <value>
# Tuc la "binPath=" va gia tri PHAI la 2 argv tach biet.
# PowerShell se truyen moi token ben duoi thanh 1 argv rieng,
# va tu them dau " bao quanh nhung argv co khoang trang.
Write-Host "Running: sc.exe create $ServiceName binPath= `"$fullExe`" start= auto DisplayName= `"Mail Sender Service`""

& sc.exe create $ServiceName binPath= $fullExe start= auto DisplayName= "Mail Sender Service"
$createCode = $LASTEXITCODE
if ($createCode -ne 0) {
    throw "sc.exe create failed with exit code $createCode"
}

& sc.exe description $ServiceName "Background bulk mail sender (US UTC window)."
$descCode = $LASTEXITCODE
if ($descCode -ne 0) {
    Write-Host "Warning: sc.exe description failed with exit code $descCode"
}

& sc.exe start $ServiceName
$startCode = $LASTEXITCODE
if ($startCode -ne 0) {
    throw "sc.exe start failed with exit code $startCode"
}

Write-Host "Service $ServiceName da duoc cai va start."
