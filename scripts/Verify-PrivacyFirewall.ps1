$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$Bin = Join-Path $Root "bin"
$Out = Join-Path $Bin "PrivacyFirewallProbe.exe"
$AdBlockerOut = Join-Path $Bin "AdBlockerProbe.exe"
$LayoutOut = Join-Path $Bin "LayoutProbe.exe"
$Csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"

if (!(Test-Path $Csc)) {
    throw "Could not find .NET Framework C# compiler at $Csc"
}

New-Item -ItemType Directory -Force $Bin | Out-Null

& $Csc /nologo /target:exe /platform:x64 `
    /out:$Out `
    /reference:System.dll `
    (Join-Path $Root "src\PrivacyFirewall.cs") `
    (Join-Path $Root "tests\PrivacyFirewallProbe.cs")

if ($LASTEXITCODE -ne 0) {
    throw "C# compiler failed with exit code $LASTEXITCODE."
}

& $Out
if ($LASTEXITCODE -ne 0) {
    throw "Privacy firewall probe failed with exit code $LASTEXITCODE."
}

& $Csc /nologo /target:exe /platform:x64 `
    /out:$AdBlockerOut `
    /reference:System.dll `
    (Join-Path $Root "src\AdBlocker.cs") `
    (Join-Path $Root "tests\AdBlockerProbe.cs")

if ($LASTEXITCODE -ne 0) {
    throw "Ad blocker probe compilation failed with exit code $LASTEXITCODE."
}

& $AdBlockerOut
if ($LASTEXITCODE -ne 0) {
    throw "Ad blocker probe failed with exit code $LASTEXITCODE."
}

& $Csc /nologo /target:exe /platform:x64 `
    /out:$LayoutOut `
    /reference:System.dll `
    /reference:System.Drawing.dll `
    /reference:System.Windows.Forms.dll `
    (Join-Path $Root "src\BorderlessTabControl.cs") `
    (Join-Path $Root "tests\LayoutProbe.cs")

if ($LASTEXITCODE -ne 0) {
    throw "Layout probe compilation failed with exit code $LASTEXITCODE."
}

& $LayoutOut
if ($LASTEXITCODE -ne 0) {
    throw "Layout probe failed with exit code $LASTEXITCODE."
}
