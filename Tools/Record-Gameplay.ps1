param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\6000.3.28f1\Editor\Unity.exe',
    [string]$EvidenceDirectory,
    [string]$Ffmpeg,
    [string]$TestFilter = 'NivelCero.Tests.FullRoutePlaythroughTests',
    [ValidateRange(30,1800)][int]$SetupTimeoutSeconds = 900,
    [ValidateRange(15,1800)][int]$CaptureTimeoutSeconds = 600
)
$ErrorActionPreference = 'Stop'
$repositoryPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$projectPath = Join-Path $repositoryPath 'Unity'
if ([String]::IsNullOrWhiteSpace($EvidenceDirectory)) { $EvidenceDirectory = Join-Path $repositoryPath ('Evidence/Gameplay3D-' + (Get-Date -Format 'yyyyMMdd-HHmmss')) }
$evidencePath = [IO.Path]::GetFullPath($EvidenceDirectory)
$resultsPath = Join-Path $repositoryPath 'Tests/results'
if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw "Unity Editor not found: $UnityEditor" }
if ([String]::IsNullOrWhiteSpace($Ffmpeg)) {
    $ffmpegCommand = Get-Command ffmpeg.exe -ErrorAction SilentlyContinue
    if ($ffmpegCommand) { $Ffmpeg = $ffmpegCommand.Source }
    else {
        $wingetPath = Join-Path $env:LOCALAPPDATA 'Microsoft/WinGet/Packages'
        $candidate = Get-ChildItem -LiteralPath $wingetPath -Directory -Filter 'Gyan.FFmpeg*' -ErrorAction SilentlyContinue |
            ForEach-Object { Get-ChildItem -LiteralPath $_.FullName -File -Filter ffmpeg.exe -Recurse } | Select-Object -First 1
        if ($candidate) { $Ffmpeg = $candidate.FullName }
    }
}
if ([String]::IsNullOrWhiteSpace($Ffmpeg) -or !(Test-Path -LiteralPath $Ffmpeg -PathType Leaf)) { throw 'Supply an installed FFmpeg executable; this helper installs nothing.' }
New-Item -ItemType Directory -Force -Path $evidencePath,$resultsPath | Out-Null
$readyPath = Join-Path $evidencePath 'recording-ready.txt'
$wavPath = Join-Path $evidencePath 'gameplay.wav'
$videoPath = Join-Path $evidencePath 'gameplay.mp4'
$frameManifestPath = Join-Path $evidencePath 'frame-manifest.json'
$framesPath = Join-Path $evidencePath 'frames'
$concatPath = Join-Path $evidencePath 'unity-frames.ffconcat'
$resultStem = if ($TestFilter -match 'Academic') { 'gameplay-recording-academic' } else { 'gameplay-recording' }
$testResults = Join-Path $resultsPath ($resultStem + '.xml')
$unityLog = Join-Path $resultsPath ($resultStem + '-unity.log')
$encoderLog = Join-Path $resultsPath ($resultStem + '-ffmpeg.log')
$metadataPath = Join-Path $evidencePath 'recording.json'
if ((Test-Path -LiteralPath $framesPath) -and @(Get-ChildItem -LiteralPath $framesPath -Filter 'frame-*.png' -File).Count -gt 0) {
    throw 'Use a fresh evidence directory; existing Unity frame sequences are never mixed or deleted.'
}
foreach ($stalePath in @($readyPath, $testResults, $metadataPath, $frameManifestPath)) {
    if (Test-Path -LiteralPath $stalePath) { Remove-Item -LiteralPath $stalePath }
}
function Quote-WindowsArgument([string]$Value) {
    if ($Value -notmatch '[\s"]' -and $Value.Length -gt 0) { return $Value }
    $escaped = [regex]::Replace($Value, '(\\*)"', '$1$1\"')
    $escaped = [regex]::Replace($escaped, '(\\+)$', '$1$1')
    return '"' + $escaped + '"'
}
function Join-ProcessArguments([string[]]$Values) { return (($Values | ForEach-Object { Quote-WindowsArgument $_ }) -join ' ') }
function Run-Ffmpeg([string[]]$Arguments, [int]$TimeoutSeconds = 300) {
    $startInfo = New-Object Diagnostics.ProcessStartInfo
    $startInfo.FileName = $Ffmpeg; $startInfo.Arguments = Join-ProcessArguments $Arguments
    $startInfo.UseShellExecute = $false; $startInfo.CreateNoWindow = $true; $startInfo.RedirectStandardError = $true
    $encoder = New-Object Diagnostics.Process; $encoder.StartInfo = $startInfo
    if (!$encoder.Start()) { throw 'FFmpeg could not start.' }
    $diagnostics = $encoder.StandardError.ReadToEndAsync()
    if (!$encoder.WaitForExit($TimeoutSeconds * 1000)) { $encoder.Kill(); throw 'FFmpeg reached the bounded encoding timeout.' }
    [IO.File]::AppendAllText($encoderLog, $diagnostics.GetAwaiter().GetResult() + "`r`n")
    if ($encoder.ExitCode -ne 0) { throw "FFmpeg failed with exit code $($encoder.ExitCode); inspect $encoderLog" }
}
$oldRecordMode = $env:NIVEL_CERO_RECORD_GAMEPLAY
$oldEvidenceDirectory = $env:NIVEL_CERO_EVIDENCE_DIR
$editorProcess = $null
try {
    $env:NIVEL_CERO_RECORD_GAMEPLAY = '1'; $env:NIVEL_CERO_EVIDENCE_DIR = $evidencePath
    $arguments = @('-batchmode','-projectPath',$projectPath,'-runTests','-testPlatform','StandaloneWindows64',
        '-buildTarget','Win64','-testFilter',$TestFilter,
        '-buildPlayerPath',(Join-Path $projectPath 'Builds/Recording/NivelCeroRecording.exe'),
        '-testResults',$testResults,'-logFile',$unityLog)
    $editorProcess = Start-Process -FilePath $UnityEditor -ArgumentList (Join-ProcessArguments $arguments) -WindowStyle Hidden -PassThru
    $started = [Diagnostics.Stopwatch]::StartNew(); $readyAt = $null
    Write-Output 'Building standalone test. Unity records its own framebuffer and game AudioListener; no desktop capture.'
    while (!$editorProcess.HasExited) {
        if ($null -eq $readyAt -and (Test-Path -LiteralPath $readyPath)) {
            $readyAt = $started.Elapsed.TotalSeconds
            Write-Output 'Unity framebuffer capture started; waiting for the complete route test.'
        }
        if ($null -eq $readyAt -and $started.Elapsed.TotalSeconds -gt $SetupTimeoutSeconds) { throw 'Timed out building/starting the recording test.' }
        if ($null -ne $readyAt -and ($started.Elapsed.TotalSeconds - $readyAt) -gt $CaptureTimeoutSeconds) { throw 'The recording test reached the bounded timeout.' }
        Start-Sleep -Milliseconds 250
    }
    if (!(Test-Path -LiteralPath $testResults)) { throw "Unity produced no test results; inspect $unityLog" }
    [xml]$testXml = Get-Content -LiteralPath $testResults -Raw
    if ($editorProcess.ExitCode -ne 0 -or $testXml.'test-run'.result -ne 'Passed' -or [int]$testXml.'test-run'.failed -ne 0) {
        throw "Recorded route test failed; inspect $testResults and $unityLog"
    }
    if (!(Test-Path -LiteralPath $frameManifestPath)) { throw 'Unity did not finalize its framebuffer manifest.' }
    $manifest = Get-Content -LiteralPath $frameManifestPath -Raw | ConvertFrom-Json
    if (!$manifest.complete -or $manifest.frameCount -le 0 -or $manifest.error) { throw 'Unity framebuffer capture is incomplete or failed.' }
    $frames = @($manifest.frames)
    if ($frames.Count -ne [int]$manifest.frameCount) { throw 'Frame count does not match the Unity manifest.' }
    if (!(Test-Path -LiteralPath $wavPath) -or (Get-Item -LiteralPath $wavPath).Length -le 44) { throw 'The game AudioListener WAV contains no samples.' }
    $culture = [Globalization.CultureInfo]::InvariantCulture
    $concat = New-Object Text.StringBuilder
    [void]$concat.AppendLine('ffconcat version 1.0')
    for ($index = 0; $index -lt $frames.Count; $index++) {
        $frame = $frames[$index]
        if ($frame.file -notmatch '^frame-\d{6}\.png$') { throw 'Unexpected frame filename in the Unity manifest.' }
        if (!(Test-Path -LiteralPath (Join-Path $framesPath $frame.file) -PathType Leaf)) { throw "Missing Unity frame $($frame.file)" }
        $end = if ($index + 1 -lt $frames.Count) { [double]$frames[$index + 1].wallSeconds } else { [double]$manifest.wallDurationSeconds }
        $duration = [Math]::Max(1.0 / 240.0, $end - [double]$frame.wallSeconds)
        [void]$concat.AppendLine("file 'frames/$($frame.file)'")
        [void]$concat.AppendLine('duration ' + $duration.ToString('0.000000', $culture))
    }
    [void]$concat.AppendLine("file 'frames/$($frames[-1].file)'")
    [IO.File]::WriteAllText($concatPath, $concat.ToString(), (New-Object Text.UTF8Encoding($false)))
    # Preserve observed wall time, including any PNG-encoding stalls, to align the game's WAV.
    # Output is 30 fps CFR; repeated frames represent actual capture stalls rather than speeding gameplay up.
    $audioOffset = [Math]::Max(0, [double]$frames[0].wallSeconds).ToString('0.000000', $culture)
    Write-Output "Encoding $($frames.Count) Unity-rendered PNG frames with the game's own WAV."
    Run-Ffmpeg -Arguments @('-hide_banner','-loglevel','warning','-nostats','-y','-safe','1','-f','concat','-i',$concatPath,
        '-ss',$audioOffset,'-i',$wavPath,'-map','0:v:0','-map','1:a:0','-vf','scale=trunc(iw/2)*2:trunc(ih/2)*2',
        '-r','30','-fps_mode','cfr','-c:v','libx264','-preset','veryfast','-crf','20','-pix_fmt','yuv420p',
        '-c:a','aac','-b:a','160k','-shortest','-movflags','+faststart',$videoPath)
    Run-Ffmpeg -Arguments @('-hide_banner','-loglevel','error','-i',$videoPath,'-f','null','-')
    [ordered]@{
        kind='Actual Unity framebuffer recording; automated keyboard/mouse route, not human playtest'
        visualSource='Only numbered PNG files captured inside Unity after WaitForEndOfFrame; includes HUD IMGUI'
        audioSource='Game AudioListener PCM, AAC mux; no microphone or system audio'
        frameCount=$frames.Count; width=$manifest.width; height=$manifest.height; outputFramesPerSecond=30
        timing='Observed frame wall timestamps preserved; CFR repeats frames when encoding stalls occurred'
        wallDurationSeconds=$manifest.wallDurationSeconds; simulationDurationSeconds=$manifest.simulationDurationSeconds
        sourceTest=$TestFilter; testResults=$testResults; mp4=$videoPath
    } | ConvertTo-Json | Set-Content -LiteralPath $metadataPath -Encoding UTF8
    Write-Output "Unity framebuffer MP4 created; route test passed and full MP4 decode passed: $videoPath"
}
finally {
    $env:NIVEL_CERO_RECORD_GAMEPLAY = $oldRecordMode; $env:NIVEL_CERO_EVIDENCE_DIR = $oldEvidenceDirectory
    if ($editorProcess -and !$editorProcess.HasExited) { $editorProcess.Kill() }
}
