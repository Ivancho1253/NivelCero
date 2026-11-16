param()
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$deliveryRoot = Join-Path $projectRoot 'Entrega'
$archivePath = Join-Path $deliveryRoot '01_Videojuego3D/Nivel_Cero_Unity.zip'
$releasePath = Join-Path $projectRoot 'Unity/Builds/Windows'
if (!(Test-Path -LiteralPath (Join-Path $releasePath 'NivelCero.exe'))) { throw 'Build the Windows release first.' }
if (Get-Process Unity -ErrorAction SilentlyContinue) { throw 'Close the project Editor before packaging its source.' }
& (Join-Path $PSScriptRoot 'Check-Project.ps1')

$windowsDelivery = Join-Path $deliveryRoot '01_Videojuego3D/Windows'
[IO.Directory]::CreateDirectory($windowsDelivery) | Out-Null
Get-ChildItem -LiteralPath $releasePath -Force | Where-Object Name -notlike '*DoNotShip*' | Copy-Item -Destination $windowsDelivery -Recurse -Force

Add-Type -AssemblyName System.IO.Compression.FileSystem
Add-Type -AssemblyName System.IO.Compression
$sourceRoots = @('Unity/Assets','Unity/Packages','Unity/ProjectSettings','Tools','Docs','Tests')
$sourceFiles = foreach ($relativeRoot in $sourceRoots) {
    Get-ChildItem -LiteralPath (Join-Path $projectRoot $relativeRoot) -Recurse -File | Where-Object {
        $_.FullName -notlike '*\Tools\ArtifactBuild\*' -and
        $_.FullName -notlike '*\Tests\results\*' -and
        $_.Extension -notin @('.log','.tmp') -and
        $_.Name -notlike 'PerformanceTestRunInfo*' -and
        $_.Name -notlike 'PerformanceTestRunSettings*'
    }
}
$rootFiles = @('README.md','.gitignore','.gitattributes','CHANGELOG.txt','game.js','icon.svg','index.html','JUGAR_NIVEL_CERO.bat','LEEME_PRIMERO.txt','menu.png','style.css','JUGAR_VERSION_UNITY.bat')
$sourceFiles += $rootFiles | ForEach-Object { Get-Item -LiteralPath (Join-Path $projectRoot $_) }
$stream = [IO.File]::Open($archivePath,[IO.FileMode]::Create,[IO.FileAccess]::ReadWrite)
$zip = New-Object IO.Compression.ZipArchive($stream,[IO.Compression.ZipArchiveMode]::Create,$false)
try {
    foreach ($sourceFile in ($sourceFiles | Sort-Object FullName -Unique)) {
        $entryName = $sourceFile.FullName.Substring($projectRoot.Length + 1).Replace('\','/')
        [IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip,$sourceFile.FullName,$entryName,[IO.Compression.CompressionLevel]::Optimal) | Out-Null
    }
} finally { $zip.Dispose(); $stream.Dispose() }
$check = [IO.Compression.ZipFile]::OpenRead($archivePath)
try {
    foreach ($required in @('Unity/Packages/manifest.json','Unity/ProjectSettings/ProjectVersion.txt','Unity/Assets/NivelCero/Scenes/SectorDeArranque3D.unity','Unity/Assets/NivelCero/Scenes/AE4_2D_SectorDeArranque.unity','Tools/Art/Source/NivelCero_OriginalArt.blend')) {
        if (!$check.GetEntry($required)) { throw "Missing ZIP entry: $required" }
    }
    if ($check.Entries.FullName | Where-Object { $_ -match '^Unity/(Library|Temp|Builds)/' }) { throw 'Source ZIP contains generated Unity directories.' }
    Write-Output "Source ZIP verified: $($check.Entries.Count) entries."
} finally { $check.Dispose() }

$manifestPath = Join-Path $deliveryRoot 'SHA256SUMS.txt'
$manifest = Get-ChildItem -LiteralPath $deliveryRoot -Recurse -File | Where-Object FullName -ne $manifestPath | Sort-Object FullName | ForEach-Object {
    $relativeName = $_.FullName.Substring($deliveryRoot.Length + 1).Replace('\','/')
    '{0}  {1}' -f (Get-FileHash -LiteralPath $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant(),$relativeName
}
[IO.File]::WriteAllLines($manifestPath,$manifest,(New-Object Text.UTF8Encoding($false)))
Write-Output "Delivery manifest: $manifestPath"
