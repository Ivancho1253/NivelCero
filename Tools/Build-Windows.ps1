param([Parameter(Mandatory=$true)][string]$UnityEditor)
$ErrorActionPreference = 'Stop'
if (!(Test-Path -LiteralPath $UnityEditor -PathType Leaf)) { throw 'Indicar la ruta real a Unity.exe; este script no instala Unity.' }
$projectPath = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../Unity'))
$logPath = Join-Path $projectPath 'build-windows.log'
& $UnityEditor -batchmode -quit -projectPath $projectPath -buildTarget Win64 -executeMethod NivelCero.Editor.ProjectSetup.BuildWindows -logFile $logPath
if ($LASTEXITCODE -ne 0) { throw "Unity devolvió $LASTEXITCODE. Revisar $logPath" }
$exePath = Join-Path $projectPath 'Builds/Windows/NivelCero.exe'
if (!(Test-Path -LiteralPath $exePath)) { throw 'Unity no generó el ejecutable esperado.' }
Write-Output "Compilación generada: $exePath. Falta probar gameplay fuera del Editor."
