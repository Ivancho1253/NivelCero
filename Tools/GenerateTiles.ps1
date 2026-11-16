param([string]$OutputPath = (Join-Path $PSScriptRoot '../Unity/Assets/NivelCero/Art/Academic/tiles.png'))
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
$bitmap = New-Object System.Drawing.Bitmap 256,32
$g = [System.Drawing.Graphics]::FromImage($bitmap)
$g.Clear([System.Drawing.Color]::Transparent)
function Fill([string]$color, [int]$x, [int]$y, [int]$w, [int]$h) {
    $brush = New-Object System.Drawing.SolidBrush ([System.Drawing.ColorTranslator]::FromHtml($color))
    $g.FillRectangle($brush,$x,$y,$w,$h)
    $brush.Dispose()
}
# Original, code-authored atlas. Each 32px cell has a distinct gameplay role.
Fill '#162536' 0 0 32 32
Fill '#294157' 1 2 30 28
Fill '#0B1723' 3 8 26 2
Fill '#0B1723' 3 22 26 2
Fill '#67DDE6' 1 0 30 2
Fill '#9EB0BC' 3 4 2 2
Fill '#9EB0BC' 27 26 2 2
Fill '#8A2945' 32 0 32 32
Fill '#F95B83' 32 0 32 3
for($i=0;$i -lt 4;$i++){Fill '#D23C60' (34+$i*8) 8 4 18}
# Zero silhouette with helmet, visor, torso, arms and boots.
Fill '#34566B' 74 2 13 10
Fill '#54EEFF' 76 5 11 3
Fill '#233C52' 72 13 17 11
Fill '#79DEE7' 77 14 6 5
Fill '#47738B' 68 14 4 10
Fill '#47738B' 89 14 4 10
Fill '#233C52' 73 24 6 8
Fill '#233C52' 83 24 6 8
# Bit.
Fill '#2E5B69' 102 9 20 14
Fill '#7EF9E0' 105 6 14 20
Fill '#16364A' 106 13 12 7
Fill '#D0FFF2' 108 14 3 3
Fill '#D0FFF2' 115 14 3 3
# Code fragment diamond.
for($i=0;$i -lt 12;$i++) { $span = if($i -lt 6){$i+1}else{12-$i}; Fill '#FFE19A' (144-$span) (4+$i*2) ($span*2) 2 }
Fill '#FFFFFF' 142 11 3 10
# Guardian: armored core, limbs, eye.
Fill '#273848' 166 6 20 22
Fill '#45586D' 162 9 5 18
Fill '#45586D' 185 9 5 18
Fill '#D64378' 171 10 10 11
Fill '#FFD3E1' 174 12 4 6
Fill '#1B2635' 167 27 7 5
Fill '#1B2635' 179 27 7 5
# Portal arch.
Fill '#214956' 195 2 4 30
Fill '#214956' 217 2 4 30
Fill '#214956' 198 1 20 4
Fill '#76F4EA' 200 5 16 27
Fill '#224E63' 203 8 10 24
# Distant server slab and illuminated seams.
Fill '#101D2E' 227 1 26 31
Fill '#183047' 230 4 20 28
Fill '#357084' 233 7 2 18
Fill '#294E68' 246 5 2 22
$resolved = [System.IO.Path]::GetFullPath($OutputPath)
[System.IO.Directory]::CreateDirectory([System.IO.Path]::GetDirectoryName($resolved)) | Out-Null
$bitmap.Save($resolved,[System.Drawing.Imaging.ImageFormat]::Png)
$g.Dispose()
$bitmap.Dispose()
Write-Output "Generated original 256x32 tilesheet: $resolved"
