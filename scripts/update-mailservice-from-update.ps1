param(
    [string]$ServiceName = "MailSenderService",
    [string]$SourceDir = "E:\Project\Automation\MailSenderService.update",
    [string]$TargetDir = "E:\Project\Automation\MailSenderService"
)

$ErrorActionPreference = "Stop"

function Test-Admin {
    $current = [Security.Principal.WindowsIdentity]::GetCurrent()
    $principal = New-Object Security.Principal.WindowsPrincipal($current)
    return $principal.IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
}

if (-not (Test-Admin)) {
    Write-Host "Relaunching with Administrator privileges..."
    $argsList = @(
        "-NoProfile",
        "-ExecutionPolicy", "Bypass",
        "-File", "`"$PSCommandPath`"",
        "-ServiceName", "`"$ServiceName`"",
        "-SourceDir", "`"$SourceDir`"",
        "-TargetDir", "`"$TargetDir`""
    )
    Start-Process -FilePath "powershell.exe" -Verb RunAs -ArgumentList $argsList | Out-Null
    exit 0
}

if (-not (Test-Path $SourceDir)) {
    throw "SourceDir not found: $SourceDir"
}
if (-not (Test-Path $TargetDir)) {
    throw "TargetDir not found: $TargetDir"
}

Write-Host "Stopping service $ServiceName..."
sc.exe stop $ServiceName | Out-Host
Start-Sleep -Seconds 3

Write-Host "Copying updated binaries..."
robocopy $SourceDir $TargetDir /E /NFL /NDL /NJH /NJS /NP | Out-Null
if ($LASTEXITCODE -gt 7) {
    throw "Robocopy failed with exit code $LASTEXITCODE"
}

Write-Host "Starting service $ServiceName..."
sc.exe start $ServiceName | Out-Host
Start-Sleep -Seconds 2

$exe = Join-Path $TargetDir "MailSender.exe"
$file = Get-Item $exe
Write-Host "Updated binary:"
Write-Host "  $($file.FullName)"
Write-Host "  LastWriteTime: $($file.LastWriteTime)"
Write-Host "  Length: $($file.Length)"

Write-Host "Service status:"
sc.exe query $ServiceName | Out-Host
