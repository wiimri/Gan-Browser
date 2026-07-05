$ErrorActionPreference = "Stop"
$Target = Join-Path $env:LOCALAPPDATA "GXLightBrowser\filters.txt"
$Dir = Split-Path -Parent $Target
New-Item -ItemType Directory -Force $Dir | Out-Null

$Lists = @(
    "https://easylist.to/easylist/easylist.txt",
    "https://easylist.to/easylist/easyprivacy.txt",
    "https://raw.githubusercontent.com/brave/adblock-lists/master/brave-lists/brave-firstparty-cname.txt"
)

"! Gan Browser filter bundle" | Set-Content -Path $Target -Encoding UTF8
"! Updated $(Get-Date -Format s)" | Add-Content -Path $Target -Encoding UTF8

foreach ($Url in $Lists) {
    Write-Host "Fetching $Url"
    $Content = Invoke-WebRequest -Uri $Url -UseBasicParsing
    "" | Add-Content -Path $Target -Encoding UTF8
    "! Source: $Url" | Add-Content -Path $Target -Encoding UTF8
    $Content.Content | Add-Content -Path $Target -Encoding UTF8
}

# YouTube-specific cosmetic filter rules (enhanced coverage beyond default lists)
"! --- YouTube-specific rules (Gan Browser enhanced) ---" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##.ytp-ad-module" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##.video-ads" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-ad-slot-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-promoted-video-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-display-ad-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-companion-slot-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-video-masthead-ad-v3-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-statement-banner-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-action-companion-ad-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-in-feed-ad-layout-renderer" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##.ytp-ad-overlay-container" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##.ytp-ad-player-overlay" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##.ytp-ad-image-overlay" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-engagement-panel-section-list-renderer[target-id=""engagement-panel-ads""]" | Add-Content -Path $Target -Encoding UTF8
"youtube.com##ytd-player-legacy-desktop-watch-ads-renderer" | Add-Content -Path $Target -Encoding UTF8
"||youtube.com/youtubei/v1/player/get_download_playback^" | Add-Content -Path $Target -Encoding UTF8
"||youtube.com/api/stats/atr^" | Add-Content -Path $Target -Encoding UTF8

Write-Host "Updated $Target with $($Lists.Count) sources + YouTube-specific rules"
