param(
    [string]$HtmlPath = (Join-Path $PSScriptRoot '../Entrega/06_Presentacion/index.html'),
    [string]$OutputPath = (Join-Path $PSScriptRoot '../Entrega/06_Presentacion/Nivel_Cero_Presentacion.pdf'),
    [string]$BrowserPath,
    [string]$PdfInfoPath,
    [string]$PdfToPpmPath
)
$ErrorActionPreference = 'Stop'

function Find-Browser {
    $candidates = New-Object System.Collections.Generic.List[string]
    foreach ($key in @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\chrome.exe',
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe',
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\msedge.exe'
    )) {
        $entry = Get-ItemProperty -LiteralPath $key -ErrorAction SilentlyContinue
        if ($entry -and $entry.'(default)') { $candidates.Add($entry.'(default)') }
    }
    foreach ($folder in @(${env:ProgramFiles},${env:ProgramFiles(x86)},$env:LOCALAPPDATA)) {
        if (-not $folder) { continue }
        foreach ($relative in @('Google/Chrome/Application/chrome.exe','Microsoft/Edge/Application/msedge.exe')) { $candidates.Add((Join-Path $folder $relative)) }
    }
    foreach ($candidate in $candidates) { if (Test-Path -LiteralPath $candidate -PathType Leaf) { return [IO.Path]::GetFullPath($candidate) } }
    throw 'Chrome/Edge was not found in App Paths or standard locations. Pass -BrowserPath explicitly.'
}
function Find-Poppler([string]$commandName) {
    $command = Get-Command $commandName -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $packageRoot = Join-Path $env:LOCALAPPDATA 'Microsoft/WinGet/Packages'
    if (Test-Path -LiteralPath $packageRoot) {
        foreach ($package in Get-ChildItem -LiteralPath $packageRoot -Directory -Filter '*Poppler*') {
            $match = Get-ChildItem -LiteralPath $package.FullName -Recurse -File -Filter $commandName | Select-Object -First 1
            if ($match) { return $match.FullName }
        }
    }
    throw "Poppler $commandName was not found. Add Poppler to PATH or pass its executable path."
}

$resolvedHtml = (Resolve-Path -LiteralPath $HtmlPath).Path
$resolvedOutput = [IO.Path]::GetFullPath($OutputPath)
if (-not $BrowserPath) { $BrowserPath = Find-Browser }
if (-not $PdfInfoPath) { $PdfInfoPath = Find-Poppler 'pdfinfo.exe' }
if (-not $PdfToPpmPath) { $PdfToPpmPath = Find-Poppler 'pdftoppm.exe' }
foreach ($executable in @($BrowserPath,$PdfInfoPath,$PdfToPpmPath)) { if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) { throw "Missing executable: $executable" } }

$buildRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot 'ArtifactBuild'))
[IO.Directory]::CreateDirectory($buildRoot) | Out-Null
[IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($resolvedOutput)) | Out-Null
$profile = Join-Path $buildRoot ('presentation-profile-' + [Guid]::NewGuid().ToString('N'))
[IO.Directory]::CreateDirectory($profile) | Out-Null
$stdout = Join-Path $profile 'chrome-stdout.log'
$stderr = Join-Path $profile 'chrome-stderr.log'
$localUri = (New-Object System.Uri($resolvedHtml)).AbsoluteUri
$arguments = @(
    '--headless=new', '--no-first-run', '--no-default-browser-check',
    '--disable-extensions', '--disable-background-networking',
    ('--user-data-dir="' + $profile + '"'),
    '--window-size=1280,720', '--hide-scrollbars', '--no-pdf-header-footer',
    '--run-all-compositor-stages-before-draw', '--virtual-time-budget=5000',
    ('--print-to-pdf="' + $resolvedOutput + '"'),
    ('"' + $localUri + '"')
)
# A fresh process/profile exports only this local HTML. It never attaches to or
# closes the user's browser, and it does not disable Chrome's security sandbox.
$exportStartedUtc = [DateTime]::UtcNow
$process = Start-Process -FilePath $BrowserPath -ArgumentList $arguments -PassThru -WindowStyle Hidden -RedirectStandardOutput $stdout -RedirectStandardError $stderr
if (-not $process.WaitForExit(60000)) {
    Stop-Process -Id $process.Id -ErrorAction SilentlyContinue
    throw "The isolated export process timed out. Logs: $profile"
}
if (-not (Test-Path -LiteralPath $resolvedOutput -PathType Leaf)) { throw "Chrome did not produce a PDF. Inspect $stderr" }
if ((Get-Item -LiteralPath $resolvedOutput).LastWriteTimeUtc -lt $exportStartedUtc.AddSeconds(-1)) { throw 'The PDF predates this export; an old output cannot count as a successful export.' }
if ((Get-Item -LiteralPath $resolvedOutput).Length -lt 10000) { throw 'The generated PDF is unexpectedly small.' }
$info = & $PdfInfoPath $resolvedOutput 2>&1
if ($LASTEXITCODE -ne 0) { throw "pdfinfo rejected the generated PDF: $info" }
$pageLine = $info | Where-Object { $_ -match '^Pages:\s+(\d+)' } | Select-Object -First 1
if (-not $pageLine -or $pageLine -notmatch '^Pages:\s+14\s*$') { throw "Expected exactly 14 pages. pdfinfo reported: $pageLine" }
$reviewRoot = Join-Path $buildRoot 'PresentationRender'
[IO.Directory]::CreateDirectory($reviewRoot) | Out-Null
& $PdfToPpmPath -r 72 -png $resolvedOutput (Join-Path $reviewRoot 'slide')
if ($LASTEXITCODE -ne 0) { throw 'Poppler failed to render presentation review PNGs.' }
$rendered = @(Get-ChildItem -LiteralPath $reviewRoot -File -Filter 'slide-*.png')
if ($rendered.Count -ne 14) { throw "Expected 14 rendered review pages; found $($rendered.Count)." }
$info | Set-Content -LiteralPath (Join-Path $reviewRoot 'pdfinfo.txt') -Encoding UTF8
Write-Output ('PDF_CREATED ' + $resolvedOutput)
Write-Output ('PAGES_VERIFIED 14')
Write-Output ('REVIEW_PNGS ' + $reviewRoot)
Write-Output ('ISOLATED_PROFILE ' + $profile)
