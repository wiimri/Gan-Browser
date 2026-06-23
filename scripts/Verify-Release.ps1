param(
    [string]$Version = "",
    [switch]$RequireAssets
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

$versionInfo = Get-Content (Join-Path $Root "src\VersionInfo.cs") -Raw
$installer = Get-Content (Join-Path $Root "installer\GXLightBrowser.iss") -Raw
$manifest = Get-Content (Join-Path $Root "update.json") -Raw | ConvertFrom-Json
$package = Get-Content (Join-Path $Root "package.json") -Raw | ConvertFrom-Json
$packageLockText = Get-Content (Join-Path $Root "package-lock.json") -Raw
$changelog = Get-Content (Join-Path $Root "CHANGELOG.md") -Raw

if ([string]::IsNullOrWhiteSpace($Version)) {
    $Version = [string]$manifest.version
}

$packageVersion = "$Version.0"
$checks = @(
    @{ Pass = $versionInfo.Contains("CurrentVersion = `"$Version`""); Message = "VersionInfo.CurrentVersion is not $Version." },
    @{ Pass = $versionInfo.Contains("ReleaseName = `"Gan Browser $Version`""); Message = "VersionInfo.ReleaseName is not Gan Browser $Version." },
    @{ Pass = $installer.Contains("#define MyAppVersion `"$Version`""); Message = "Installer version is not $Version." },
    @{ Pass = ([string]$manifest.version -eq $Version); Message = "update.json version is not $Version." },
    @{ Pass = ([string]$manifest.releaseName -eq "Gan Browser $Version"); Message = "update.json release name is inconsistent." },
    @{ Pass = ([string]$package.version -eq $packageVersion); Message = "package.json version is not $packageVersion." },
    @{ Pass = ([regex]::Matches($packageLockText, '"version"\s*:\s*"' + [regex]::Escape($packageVersion) + '"').Count -ge 2); Message = "package-lock.json root versions are not $packageVersion." },
    @{ Pass = $changelog.Contains("Version publicada: ``$Version``"); Message = "CHANGELOG current version is not $Version." },
    @{ Pass = $changelog.Contains("## v$Version "); Message = "CHANGELOG does not contain v$Version." },
    @{ Pass = ([string]$manifest.downloadUrl).EndsWith("/GanBrowser-Setup-x64.exe"); Message = "Permanent installer URL is inconsistent." },
    @{ Pass = ([string]$manifest.sha256Url).EndsWith("/GanBrowser-Setup-x64.sha256.txt"); Message = "Permanent SHA-256 URL is inconsistent." }
)

foreach ($check in $checks) {
    if (!$check.Pass) {
        throw $check.Message
    }
}

if ($RequireAssets) {
    $assetNames = @(
        "GanBrowser-Setup-$Version-x64.exe",
        "GanBrowser-Setup-$Version-x64.sha256.txt",
        "GanBrowser-Setup-x64.exe",
        "GanBrowser-Setup-x64.sha256.txt",
        "GXLightBrowser-Setup-x64.exe",
        "GXLightBrowser-Setup-x64.sha256.txt"
    )

    foreach ($assetName in $assetNames) {
        if (!(Test-Path (Join-Path $Root "dist\$assetName"))) {
            throw "Required release asset not found: dist\$assetName"
        }
    }

    $versionedInstaller = Join-Path $Root "dist\GanBrowser-Setup-$Version-x64.exe"
    $versionedHash = (Get-FileHash $versionedInstaller -Algorithm SHA256).Hash
    if ([string]::IsNullOrWhiteSpace([string]$manifest.sha256) -or
        ![string]::Equals([string]$manifest.sha256, $versionedHash, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "update.json inline sha256 does not match dist\GanBrowser-Setup-$Version-x64.exe."
    }
}

Write-Host "Release metadata verified for Gan Browser $Version."
