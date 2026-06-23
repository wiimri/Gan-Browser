param(
    [string]$InnoCompiler = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Prerequisites = Join-Path $Root "prerequisites"
$Dist = Join-Path $Root "dist"

& (Join-Path $PSScriptRoot "Build.ps1")

New-Item -ItemType Directory -Force $Prerequisites | Out-Null
New-Item -ItemType Directory -Force $Dist | Out-Null

$WebView2Setup = Join-Path $Prerequisites "MicrosoftEdgeWebview2Setup.exe"
$DotNet48Setup = Join-Path $Prerequisites "ndp48-web.exe"

if (!(Test-Path $WebView2Setup)) {
    Write-Host "Downloading official Microsoft WebView2 Evergreen Bootstrapper..."
    Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/p/?LinkId=2124703" -OutFile $WebView2Setup
}

if (!(Test-Path $DotNet48Setup)) {
    Write-Host "Downloading official Microsoft .NET Framework 4.8 web installer..."
    Invoke-WebRequest -Uri "https://go.microsoft.com/fwlink/?linkid=2088631" -OutFile $DotNet48Setup
}

if ([string]::IsNullOrWhiteSpace($InnoCompiler)) {
    $Candidates = @(
        "$env:LOCALAPPDATA\Programs\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe"
    )
    $InnoCompiler = $Candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}

if ([string]::IsNullOrWhiteSpace($InnoCompiler) -or !(Test-Path $InnoCompiler)) {
    throw "Inno Setup 6 no esta instalado. Instala Inno Setup y vuelve a ejecutar scripts\Build-Installer.ps1."
}

& $InnoCompiler (Join-Path $Root "installer\GXLightBrowser.iss")
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup fallo con codigo $LASTEXITCODE."
}

$VersionedInstaller = Get-ChildItem $Dist -Filter "GanBrowser-Setup-*-x64.exe" |
    Sort-Object LastWriteTime -Descending |
    Select-Object -First 1
if ($null -ne $VersionedInstaller) {
    $LatestInstaller = Join-Path $Dist "GanBrowser-Setup-x64.exe"
    $LegacyLatestInstaller = Join-Path $Dist "GXLightBrowser-Setup-x64.exe"
    Copy-Item $VersionedInstaller.FullName $LatestInstaller -Force
    Copy-Item $VersionedInstaller.FullName $LegacyLatestInstaller -Force

    $VersionedHash = (Get-FileHash $VersionedInstaller.FullName -Algorithm SHA256).Hash
    $LatestHash = (Get-FileHash $LatestInstaller -Algorithm SHA256).Hash
    $LegacyLatestHash = (Get-FileHash $LegacyLatestInstaller -Algorithm SHA256).Hash
    Set-Content -Path (Join-Path $Dist ($VersionedInstaller.BaseName + ".sha256.txt")) `
        -Value ($VersionedHash + "  " + $VersionedInstaller.Name) -Encoding ASCII
    Set-Content -Path (Join-Path $Dist "GanBrowser-Setup-x64.sha256.txt") `
        -Value ($LatestHash + "  GanBrowser-Setup-x64.exe") -Encoding ASCII
    Set-Content -Path (Join-Path $Dist "GXLightBrowser-Setup-x64.sha256.txt") `
        -Value ($LegacyLatestHash + "  GXLightBrowser-Setup-x64.exe") -Encoding ASCII

    # Inyectar SHA-256 inline en update.json
    $UpdateJsonPath = Join-Path $Root "update.json"
    if (Test-Path $UpdateJsonPath) {
        $UpdateJson = Get-Content $UpdateJsonPath -Raw -Encoding UTF8
        $UpdateJson = $UpdateJson -replace '"sha256":\s*"[^"]*"', ('"sha256": "' + $VersionedHash + '"')
        [System.IO.File]::WriteAllText($UpdateJsonPath, ($UpdateJson.TrimEnd() + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Write-Host "Injected SHA-256 $VersionedHash into update.json"
    }
}

Write-Host "Installer created in $Dist"
