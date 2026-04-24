#Requires -Version 7.0
$ErrorActionPreference = 'Stop'

$root       = $PSScriptRoot
$appSvg     = Join-Path $root 'ContextMenuManager\Properties\AppIcon.svg'
$propsDir   = Join-Path $root 'ContextMenuManager\Properties'
$ico        = Join-Path $propsDir 'AppIcon.ico'
$logoPng    = Join-Path $propsDir 'Resources\Images\Logo.png'

$missing = @()
if (-not (Get-Command resvg -ErrorAction SilentlyContinue)) { $missing += 'resvg' }
if (-not (Get-Command magick -ErrorAction SilentlyContinue)) { $missing += 'magick' }
if ($missing.Count) {
    Write-Host "Missing required tool(s): $($missing -join ', ')" -ForegroundColor Red
    if ($missing -contains 'resvg') {
        Write-Host "Install resvg: https://github.com/linebender/resvg/releases  (or: cargo install resvg)" -ForegroundColor Yellow
    }
    if ($missing -contains 'magick') {
        Write-Host "Install ImageMagick: https://imagemagick.org/script/download.php#windows  (or: winget install ImageMagick.ImageMagick)" -ForegroundColor Yellow
    }
    exit 1
}

if (-not (Test-Path $appSvg)) {
    Write-Host "SVG not found: $appSvg" -ForegroundColor Red
    exit 1
}

$icoSizes = 16, 20, 24, 32, 40, 48, 64, 96, 128, 256
$tempPngs = foreach ($s in $icoSizes) { Join-Path $propsDir "icon_$s.png" }

$iconTasks = @()
for ($i = 0; $i -lt $icoSizes.Count; $i++) {
    $iconTasks += [pscustomobject]@{ Size = $icoSizes[$i]; Output = $tempPngs[$i] }
}
$iconTasks += [pscustomobject]@{ Size = 256; Output = $logoPng }

Write-Host "=== Rasterizing app icon (parallel) ==="
$iconTasks | ForEach-Object -ThrottleLimit 16 -Parallel {
    $ErrorActionPreference = 'Stop'
    $t = $_
    & resvg $using:appSvg $t.Output -w $t.Size -h $t.Size
    if ($LASTEXITCODE -ne 0) { throw "resvg failed for $($t.Output)" }
}

Write-Host "=== Packing AppIcon.ico ==="
& magick @tempPngs -define png:exclude-chunks=date,time $ico
if ($LASTEXITCODE -ne 0) {
    Write-Host "magick failed to pack AppIcon.ico." -ForegroundColor Red
    exit 1
}
$tempPngs | Where-Object { Test-Path $_ } | Remove-Item

$bannerTasks = @(
    [pscustomobject]@{ Svg = (Join-Path $root 'Logo\Logo.svg');    Png = (Join-Path $root 'Logo\Logo.png') }
    [pscustomobject]@{ Svg = (Join-Path $root 'Logo\Logo-en.svg'); Png = (Join-Path $root 'Logo\Logo-en.png') }
)

Write-Host "=== Rasterizing + trimming logo banners (parallel) ==="
$trimmed = $bannerTasks | ForEach-Object -ThrottleLimit 4 -Parallel {
    $ErrorActionPreference = 'Stop'
    $t = $_
    if (-not (Test-Path $t.Svg)) { throw "SVG not found: $($t.Svg)" }
    $tmp = "$($t.Png).tmp.png"
    & resvg $t.Svg $tmp -h 512
    if ($LASTEXITCODE -ne 0) { throw "resvg failed for $($t.Svg)" }
    & magick $tmp -trim +repage $tmp
    if ($LASTEXITCODE -ne 0) { throw "magick trim failed for $tmp" }
    $wh = (& magick identify -format "%w %h" $tmp).Trim() -split ' '
    [pscustomobject]@{ Png = $t.Png; Tmp = $tmp; W = [int]$wh[0]; H = [int]$wh[1] }
}

$maxW = ($trimmed | Measure-Object -Property W -Maximum).Maximum
$maxH = ($trimmed | Measure-Object -Property H -Maximum).Maximum
Write-Host "Normalizing banners to ${maxW}x${maxH}"

$trimmed | ForEach-Object -ThrottleLimit 4 -Parallel {
    $ErrorActionPreference = 'Stop'
    $d = $_
    & magick $d.Tmp -background none -gravity center `
        -extent "$($using:maxW)x$($using:maxH)" +repage `
        -define png:exclude-chunks=date,time $d.Png
    if ($LASTEXITCODE -ne 0) { throw "magick extent failed for $($d.Png)" }
    Remove-Item $d.Tmp
}

Write-Host "=== Done ==="
