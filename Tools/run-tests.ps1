<#
.SYNOPSIS
    Roda os testes do projeto em uma cópia espelhada, para funcionar com o Editor aberto.

.DESCRIPTION
    A Unity não deixa duas instâncias abrirem a mesma pasta. Este script espelha Assets,
    Packages e ProjectSettings para uma pasta de cópia (a Library só na primeira vez, para não
    reimportar tudo) e roda os testes nela em batchmode.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1
    powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -Filter GameplaySmokeTests
    powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -Build
#>
param(
    [string]$Filter = "",
    [switch]$Build,
    [string]$UnityExe = "",
    [string]$CopyDir = (Join-Path $env:LOCALAPPDATA "MonstersAndSoldiers2-TestCopy")
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $PSScriptRoot

# Editor: mesma versão do ProjectVersion.txt, procurada nas pastas do Unity Hub.
if (-not $UnityExe) {
    $version = (Select-String -Path "$projectDir\ProjectSettings\ProjectVersion.txt" -Pattern "m_EditorVersion: (.+)").Matches[0].Groups[1].Value.Trim()
    $roots = @("E:\Unity Installs", "C:\Program Files\Unity\Hub\Editor")
    $secondary = Join-Path $env:APPDATA "UnityHub\secondaryInstallPath.json"
    if (Test-Path $secondary) { $roots = @((Get-Content $secondary -Raw | ConvertFrom-Json)) + $roots }
    foreach ($root in $roots) {
        $candidate = Join-Path $root "$version\Editor\Unity.exe"
        if (Test-Path $candidate) { $UnityExe = $candidate; break }
    }
    if (-not $UnityExe) { throw "Unity $version não encontrado. Use -UnityExe <caminho>." }
}

# Espelha o projeto (robocopy: códigos < 8 são sucesso).
Write-Host "Sincronizando cópia em $CopyDir ..."
foreach ($dir in "Assets", "Packages", "ProjectSettings") {
    robocopy "$projectDir\$dir" "$CopyDir\$dir" /MIR /NFL /NDL /NJH /NJS /NP | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar $dir (robocopy $LASTEXITCODE)." }
}
if (-not (Test-Path "$CopyDir\Library") -and (Test-Path "$projectDir\Library")) {
    Write-Host "Primeira execução: copiando a Library (evita reimportar tudo) ..."
    # Bee guarda caminhos absolutos do projeto original; a cópia reconstrói esse cache.
    robocopy "$projectDir\Library" "$CopyDir\Library" /MIR /NFL /NDL /NJH /NJS /NP /XD Bee /XF "*lock*" | Out-Null
    if ($LASTEXITCODE -ge 8) { throw "Falha ao copiar Library (robocopy $LASTEXITCODE)." }
}

$outDir = Join-Path $CopyDir "TestOutput"
New-Item -ItemType Directory -Force $outDir | Out-Null
$results = Join-Path $outDir "editmode-results.xml"
$log = Join-Path $outDir "unity-tests.log"
Remove-Item $results -ErrorAction SilentlyContinue

$unityArgs = @("-batchmode", "-projectPath", $CopyDir, "-runTests", "-testPlatform", "EditMode",
               "-testResults", $results, "-logFile", $log)
if ($Filter) { $unityArgs += @("-testFilter", $Filter) }

Write-Host "Rodando testes com $UnityExe ..."
& $UnityExe @unityArgs | Out-Null
$testExit = $LASTEXITCODE

if (-not (Test-Path $results)) {
    Select-String -Path $log -Pattern "error CS|Aborting batchmode|Scripts have compiler errors" | ForEach-Object { Write-Host $_.Line -ForegroundColor Red }
    throw "Os testes não rodaram. Log: $log"
}

[xml]$xml = Get-Content $results -Encoding UTF8
$run = $xml.'test-run'
Write-Host ("Testes: {0} total, {1} passaram, {2} falharam" -f $run.total, $run.passed, $run.failed)
foreach ($case in $xml.SelectNodes("//test-case[@result='Failed']")) {
    Write-Host ("FALHOU {0}: {1}" -f $case.fullname, $case.failure.message.'#cdata-section'.Trim()) -ForegroundColor Red
}

if ($Build -and $testExit -eq 0) {
    $buildLog = Join-Path $outDir "unity-build.log"
    Write-Host "Gerando build de Windows ..."
    & $UnityExe -batchmode -quit -projectPath $CopyDir -buildWindows64Player (Join-Path $outDir "Build\MonstersAndSoldiers.exe") -logFile $buildLog | Out-Null
    if (Select-String -Path $buildLog -Pattern "Build Finished, Result: Success" -Quiet) {
        Write-Host "Build OK: $outDir\Build"
    } else {
        Write-Host "Build falhou. Log: $buildLog" -ForegroundColor Red
        exit 1
    }
}

exit $testExit
