$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$assetRoot = Join-Path $projectRoot 'Unity/Assets'
$manifest = Get-Content -LiteralPath (Join-Path $projectRoot 'Unity/Packages/manifest.json') -Raw | ConvertFrom-Json
if (!$manifest.dependencies.'com.unity.inputsystem') { throw 'Input System missing.' }
$guids = @{}
foreach ($meta in (Get-ChildItem -LiteralPath $assetRoot -Filter '*.meta' -Recurse -File)) {
    $match = [regex]::Match([IO.File]::ReadAllText($meta.FullName), '(?m)^guid: ([a-f0-9]{32})')
    if (!$match.Success) { throw "Invalid GUID: $($meta.FullName)" }
    $guid = $match.Groups[1].Value
    if ($guids.ContainsKey($guid)) { throw "Duplicate GUID: $guid" }
    $guids[$guid] = $meta.FullName
}
foreach ($asset in (Get-ChildItem -LiteralPath $assetRoot -File -Recurse | Where-Object Extension -ne '.meta')) {
    if (!(Test-Path -LiteralPath ($asset.FullName + '.meta'))) { throw "Missing metadata: $($asset.FullName)" }
    if ($asset.Extension -eq '.asmdef') { $null = [IO.File]::ReadAllText($asset.FullName) | ConvertFrom-Json }
}
$scenes = @(Get-ChildItem -LiteralPath (Join-Path $assetRoot 'NivelCero/Scenes') -Filter '*.unity')
if ($scenes.Count -ne 2) { throw 'Expected both 3D and academic 2D scenes.' }
foreach ($scene in $scenes) {
    $content = [IO.File]::ReadAllText($scene.FullName)
    foreach ($match in [regex]::Matches($content, 'guid: ([a-f0-9]{32})')) {
        $reference = $match.Groups[1].Value
        if ($reference.StartsWith('0000000000000000')) { continue }
        if (!$guids.ContainsKey($reference)) {
            $packageCache = Join-Path $projectRoot 'Unity/Library/PackageCache'
            $packageMatch = if (Test-Path -LiteralPath $packageCache) { & rg -l --glob '*.meta' --fixed-strings "guid: $reference" $packageCache | Select-Object -First 1 }
            if (!$packageMatch) { throw "Unresolved scene GUID $reference in $($scene.Name)" }
        }
    }
}
$originals = @('CHANGELOG.txt','game.js','icon.svg','index.html','JUGAR_NIVEL_CERO.bat','LEEME_PRIMERO.txt','menu.png','style.css')
Push-Location $projectRoot
try { & git diff --exit-code 4e8f046 -- $originals; if ($LASTEXITCODE -ne 0) { throw 'Original WebGL files changed.' } }
finally { Pop-Location }
Write-Output "PASS: package/assembly JSON, $($guids.Count) unique asset GUIDs, 2 scene reference graphs, original WebGL unchanged. Does not compile or execute Unity."
