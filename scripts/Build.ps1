param(
    [string]$WebView2Version = "1.0.3537.50"
)

$ErrorActionPreference = "Stop"
$Root = Split-Path -Parent $PSScriptRoot
$PkgRoot = Join-Path $Root "packages"
$PkgDir = Join-Path $PkgRoot "Microsoft.Web.WebView2.$WebView2Version"
$Bin = Join-Path $Root "bin"
$Nupkg = Join-Path $PkgRoot "Microsoft.Web.WebView2.$WebView2Version.nupkg"
$Url = "https://www.nuget.org/api/v2/package/Microsoft.Web.WebView2/$WebView2Version"

New-Item -ItemType Directory -Force $PkgRoot | Out-Null
New-Item -ItemType Directory -Force $Bin | Out-Null

if (!(Test-Path $PkgDir)) {
    if (!(Test-Path $Nupkg)) {
        Write-Host "Downloading Microsoft.Web.WebView2 $WebView2Version..."
        Invoke-WebRequest -Uri $Url -OutFile $Nupkg
    }

    $Zip = [System.IO.Path]::ChangeExtension($Nupkg, ".zip")
    Copy-Item $Nupkg $Zip -Force
    Expand-Archive -Path $Zip -DestinationPath $PkgDir -Force
}

$Csc = "$env:WINDIR\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (!(Test-Path $Csc)) {
    throw "Could not find .NET Framework C# compiler at $Csc"
}

$Core = Join-Path $PkgDir "lib\net462\Microsoft.Web.WebView2.Core.dll"
$WinForms = Join-Path $PkgDir "lib\net462\Microsoft.Web.WebView2.WinForms.dll"
$Loader = Join-Path $PkgDir "runtimes\win-x64\native\WebView2Loader.dll"
$WindowsSecurity = Join-Path $env:WINDIR "System32\WinMetadata\Windows.Security.winmd"
$WindowsFoundation = Join-Path $env:WINDIR "System32\WinMetadata\Windows.Foundation.winmd"
$WindowsRuntime = Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\System.Runtime.WindowsRuntime\v4.0_4.0.0.0__b77a5c561934e089\System.Runtime.WindowsRuntime.dll"
$SystemRuntime = Join-Path $env:WINDIR "Microsoft.NET\assembly\GAC_MSIL\System.Runtime\v4.0_4.0.0.0__b03f5f7f11d50a3a\System.Runtime.dll"

if (!(Test-Path $Core) -or !(Test-Path $WinForms) -or !(Test-Path $Loader)) {
    throw "The WebView2 package layout was not recognized. Check version $WebView2Version."
}

Copy-Item $Core $Bin -Force
Copy-Item $WinForms $Bin -Force
Copy-Item $Loader $Bin -Force

$Sources = Get-ChildItem (Join-Path $Root "src") -Filter "*.cs" | ForEach-Object { $_.FullName }
$Output = Join-Path $Bin "GXLightBrowser.exe"
$Icon = Join-Path $Root "assets\GanBrowser.ico"

if (!(Test-Path $Icon)) {
    & (Join-Path $PSScriptRoot "Generate-Icon.ps1") -OutputPath $Icon
}

& $Csc /nologo /target:winexe /platform:x64 /optimize+ `
    /out:$Output `
    /win32icon:$Icon `
    /reference:System.dll `
    /reference:System.Core.dll `
    /reference:System.Drawing.dll `
    /reference:System.Runtime.Serialization.dll `
    /reference:System.Security.dll `
    /reference:System.Windows.Forms.dll `
    /reference:$SystemRuntime `
    /reference:$WindowsSecurity `
    /reference:$WindowsFoundation `
    /reference:$WindowsRuntime `
    /reference:$Core `
    /reference:$WinForms `
    $Sources

if ($LASTEXITCODE -ne 0) {
    throw "C# compiler failed with exit code $LASTEXITCODE."
}

Write-Host "Built $Output"
