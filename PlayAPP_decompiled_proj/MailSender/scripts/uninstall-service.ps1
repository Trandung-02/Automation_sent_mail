param(
    [string]$ServiceName = "MailSenderService"
)

$ErrorActionPreference = "Continue"

Write-Host "Stopping service $ServiceName (if running)..."
sc.exe stop $ServiceName | Out-Null
Start-Sleep -Seconds 1

Write-Host "Deleting service $ServiceName..."
sc.exe delete $ServiceName | Out-Null

Write-Host "Done."
