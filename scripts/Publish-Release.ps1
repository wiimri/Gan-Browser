param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string[]]$Assets = @(),

    [string]$Repository = "wiimri/Gan-Browser-Releases",
    [string]$Title = "",
    [string]$Notes = ""
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot

if ($Assets.Count -eq 0) {
    $Assets = @(
        (Join-Path $Root "dist\GanBrowser-Setup-$Version-x64.exe"),
        (Join-Path $Root "dist\GanBrowser-Setup-$Version-x64.sha256.txt"),
        (Join-Path $Root "dist\GanBrowser-Setup-x64.exe"),
        (Join-Path $Root "dist\GanBrowser-Setup-x64.sha256.txt"),
        (Join-Path $Root "dist\GXLightBrowser-Setup-x64.exe"),
        (Join-Path $Root "dist\GXLightBrowser-Setup-x64.sha256.txt")
    )
}

& (Join-Path $PSScriptRoot "Verify-Release.ps1") -Version $Version -RequireAssets

# Inyectar SHA-256 inline en update.json
$VersionedInstaller = Join-Path $Root "dist\GanBrowser-Setup-$Version-x64.exe"
if (Test-Path $VersionedInstaller) {
    $Hash = (Get-FileHash $VersionedInstaller -Algorithm SHA256).Hash
    $UpdateJsonPath = Join-Path $Root "update.json"
    if (Test-Path $UpdateJsonPath) {
        $UpdateJson = Get-Content $UpdateJsonPath -Raw -Encoding UTF8
        $UpdateJson = $UpdateJson -replace '"sha256":\s*"[^"]*"', ('"sha256": "' + $Hash + '"')
        [System.IO.File]::WriteAllText($UpdateJsonPath, ($UpdateJson.TrimEnd() + [Environment]::NewLine), [System.Text.UTF8Encoding]::new($false))
        Write-Host "Injected SHA-256 $Hash into update.json"
    }
}

if ([string]::IsNullOrWhiteSpace($Title)) {
    $Title = "Gan Browser $Version"
}
if ([string]::IsNullOrWhiteSpace($Notes)) {
    $Notes = "Instalador de Gan Browser $Version para Windows 10/11 x64."
}

$credentialLines = "protocol=https`nhost=github.com`n`n" | git credential fill
$credential = @{}
foreach ($line in $credentialLines) {
    $equals = $line.IndexOf("=")
    if ($equals -gt 0) {
        $credential[$line.Substring(0, $equals)] = $line.Substring($equals + 1)
    }
}

if (!$credential.ContainsKey("password") -or [string]::IsNullOrWhiteSpace($credential["password"])) {
    throw "Git Credential Manager no entrego una credencial utilizable para GitHub."
}

$headers = @{
    Accept = "application/vnd.github+json"
    Authorization = "Bearer " + $credential["password"]
    "X-GitHub-Api-Version" = "2022-11-28"
    "User-Agent" = "GanBrowser-Release-Script"
}

$tag = "v" + $Version
$releaseApi = "https://api.github.com/repos/$Repository/releases/tags/$tag"
$release = $null
try {
    $release = Invoke-RestMethod -Uri $releaseApi -Headers $headers -Method Get
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -ne 404) {
        throw
    }
}

if ($null -eq $release) {
    $payloadJson = @{
        tag_name = $tag
        target_commitish = "main"
        name = $Title
        body = $Notes
        draft = $false
        prerelease = $false
    } | ConvertTo-Json -Compress
    $payload = [System.Text.Encoding]::UTF8.GetBytes($payloadJson)
    $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases" `
        -Headers $headers -Method Post -Body $payload -ContentType "application/json; charset=utf-8"
}

$uploadBase = $release.upload_url.Split("{")[0]
$expandedAssets = @()
foreach ($assetGroup in $Assets) {
    $expandedAssets += $assetGroup.Split(",", [System.StringSplitOptions]::RemoveEmptyEntries)
}

function Upload-ReleaseAsset {
    param(
        [string]$UploadUrl,
        [hashtable]$RequestHeaders,
        [string]$AssetPath
    )

    $attempts = 3
    for ($attempt = 1; $attempt -le $attempts; $attempt++) {
        try {
            Invoke-RestMethod -Uri $UploadUrl -Headers $RequestHeaders -Method Post `
                -ContentType "application/octet-stream" -InFile $AssetPath | Out-Null
            return
        }
        catch {
            if ($attempt -eq $attempts) {
                throw
            }
            Write-Warning "Upload attempt $attempt failed for $AssetPath. Retrying..."
            Start-Sleep -Seconds (5 * $attempt)
        }
    }
}

foreach ($asset in $expandedAssets) {
    if (!(Test-Path $asset)) {
        throw "Release asset not found: $asset"
    }
    $resolved = Resolve-Path $asset
    $name = [Uri]::EscapeDataString([System.IO.Path]::GetFileName($resolved))

    foreach ($existing in $release.assets) {
        if ($existing.name -eq [System.IO.Path]::GetFileName($resolved)) {
            Invoke-RestMethod -Uri "https://api.github.com/repos/$Repository/releases/assets/$($existing.id)" `
                -Headers $headers -Method Delete | Out-Null
        }
    }

    Write-Host "Uploading $resolved..."
    Upload-ReleaseAsset -UploadUrl ($uploadBase + "?name=" + $name) -RequestHeaders $headers -AssetPath $resolved
}

Write-Host "Published $($release.html_url)"
