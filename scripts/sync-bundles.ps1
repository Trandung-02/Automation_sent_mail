$ErrorActionPreference = "Stop"

$root = "E:\Project\Automation"
$bundleRoot = Join-Path $root "_bundles"
$managerBundle = Join-Path $bundleRoot "MailSender.Manager"
$playAppBundle = Join-Path $bundleRoot "PlayAPP"

New-Item -ItemType Directory -Path $managerBundle -Force | Out-Null
New-Item -ItemType Directory -Path $playAppBundle -Force | Out-Null

$managerFiles = @(
    "MailSender.Manager.exe",
    "MailSender.Manager.dll",
    "MailSender.Manager.pdb",
    "MailSender.Manager.deps.json",
    "MailSender.Manager.runtimeconfig.json",
    "Open-MailSender-Manager.bat",
    "Microsoft.Bcl.AsyncInterfaces.dll",
    "Microsoft.Playwright.dll",
    "Newtonsoft.Json.dll"
)

$playAppFiles = @(
    "PlayAPP.exe",
    "PlayAPP.dll",
    "PlayAPP.pdb",
    "PlayAPP.deps.json",
    "PlayAPP.runtimeconfig.json",
    "Microsoft.Bcl.AsyncInterfaces.dll",
    "Microsoft.Playwright.dll",
    "Newtonsoft.Json.dll"
)

foreach ($name in $managerFiles) {
    $src = Join-Path $root $name
    if (Test-Path $src) {
        Copy-Item $src $managerBundle -Force
    }
}

foreach ($name in $playAppFiles) {
    $src = Join-Path $root $name
    if (Test-Path $src) {
        Copy-Item $src $playAppBundle -Force
    }
}

Write-Host "Da dong bo xong _bundles." -ForegroundColor Green
