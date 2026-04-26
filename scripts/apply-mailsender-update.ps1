# Chay: PowerShell "Run as Administrator"
# Dung: MailSenderService.publish (ban publish moi) -> ghi de MailSenderService (Windows Service dang dung)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
$src = Join-Path $root "MailSenderService.publish"
$dst = Join-Path $root "MailSenderService"
$svc = "MailSenderService"

if (-not (Test-Path (Join-Path $src "MailSender.exe"))) {
    throw "Khong thay $src\MailSender.exe. Trong thu muc Automation, chay: dotnet publish PlayAPP_decompiled_proj\MailSender\MailSender.csproj -c Release -o MailSenderService.publish"
}

$isAdmin = ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole(
    [Security.Principal.WindowsBuiltInRole] "Administrator")
if (-not $isAdmin) {
    throw "Chay script bang quyen Administrator (chuot phai -> Run as administrator)."
}

Write-Host "Dang dung $svc ..."
$stopOut = & sc.exe stop $svc 2>&1
$stopCode = $LASTEXITCODE
Write-Host $stopOut
if ($stopCode -ne 0) {
    Write-Host "Luu y: sc stop exit $stopCode (thuong OK neu service dang dung/ khong cai). Tiep tuc sau 4s..."
}
Start-Sleep -Seconds 4

if (-not (Test-Path $dst)) {
    New-Item -ItemType Directory -Path $dst -Force | Out-Null
}

# Mirror: thu muc dich giong thu muc nguon (xoa file thua o dich)
$rc = & robocopy.exe $src $dst /MIR /R:2 /W:2 /NFL /NDL /NP
# robocopy: 0-7 = thanh cong; >=8 = loi
if ($LASTEXITCODE -ge 8) {
    throw "robocopy that bai, exit $LASTEXITCODE"
}

Write-Host "Dang khoi dong $svc ..."
$startOut = & sc.exe start $svc 2>&1
Write-Host $startOut
if ($LASTEXITCODE -ne 0) {
    throw "sc start that bai, exit $LASTEXITCODE"
}

Write-Host "Xong. Kiem tra: sc query $svc"
