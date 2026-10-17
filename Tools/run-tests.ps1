<#
.SYNOPSIS
    Roda os testes EditMode do projeto, sem copiar nada.

.DESCRIPTION
    Editor aberto: pede para Assets/Editor/TestBridge.cs rodar os testes no próprio Editor
    (ele dá Refresh antes, então compila o que mudou) e espera o resultado.
    Editor fechado: roda em batchmode direto na pasta do projeto.

.EXAMPLE
    powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1
    powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -Filter PuzzleSolverTests
    powershell -ExecutionPolicy Bypass -File Tools/run-tests.ps1 -Build   (só com o Editor fechado)
#>
param(
    [string]$Filter = "",
    [switch]$Build,
    [string]$UnityExe = "",
    [int]$TimeoutSec = 600
)

$ErrorActionPreference = "Stop"
$projectDir = Split-Path -Parent $PSScriptRoot

# O Editor segura Temp\UnityLockfile enquanto o projeto está aberto.
function Test-EditorOpen {
    $lock = Join-Path $projectDir "Temp\UnityLockfile"
    if (-not (Test-Path $lock)) { return $false }
    try {
        $stream = [System.IO.File]::Open($lock, "Open", "ReadWrite", "None")
        $stream.Close()
        return $false
    } catch {
        return $true
    }
}

if (Test-EditorOpen) {
    if ($Build) { throw "-Build precisa do Editor fechado." }

    $bridge = Join-Path $projectDir "Temp\TestBridge"
    $result = Join-Path $bridge "result.txt"
    New-Item -ItemType Directory -Force $bridge | Out-Null
    Remove-Item $result -ErrorAction SilentlyContinue
    [System.IO.File]::WriteAllText((Join-Path $bridge "request.txt"), $Filter)
    Write-Host "Pedido enviado ao Editor aberto. Aguardando ..."

    $deadline = (Get-Date).AddSeconds($TimeoutSec)
    $status = ""
    while ((Get-Date) -lt $deadline) {
        Start-Sleep -Milliseconds 500
        if (-not (Test-Path $result)) { continue }
        $text = Get-Content $result -Raw -Encoding UTF8
        if ($text -match "^status: (\S+)") { $status = $Matches[1] }
        if ($status -in "passed", "failed", "compile-error") { break }
    }

    if ($status -notin "passed", "failed", "compile-error") {
        throw "O Editor não respondeu em $TimeoutSec s (status: '$status'). Ele está em Play Mode ou com um diálogo aberto?"
    }

    $body = ($text -split "`n", 2)[1]
    if ($status -eq "compile-error") {
        Write-Host "Erros de compilação:" -ForegroundColor Red
        Write-Host $body -ForegroundColor Red
        exit 1
    }
    foreach ($line in $body -split "`n") {
        if ($line -like "FALHOU*") { Write-Host $line -ForegroundColor Red } elseif ($line) { Write-Host $line }
    }
    Write-Host "Resultado completo: $bridge\results.xml"
    if ($status -eq "passed") { exit 0 } else { exit 1 }
}

# ---------- Editor fechado: batchmode na própria pasta ----------

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

$outDir = Join-Path $projectDir "Temp\TestOutput"
New-Item -ItemType Directory -Force $outDir | Out-Null
$results = Join-Path $outDir "editmode-results.xml"
$log = Join-Path $outDir "unity-tests.log"
Remove-Item $results -ErrorAction SilentlyContinue

$unityArgs = @("-batchmode", "-projectPath", $projectDir, "-runTests", "-testPlatform", "EditMode",
               "-testResults", $results, "-logFile", $log)
if ($Filter) { $unityArgs += @("-testFilter", $Filter) }

Write-Host "Editor fechado: rodando testes em batchmode com $UnityExe ..."
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
    $buildDir = Join-Path $projectDir "Builds\Windows"
    Write-Host "Gerando build de Windows ..."
    & $UnityExe -batchmode -quit -projectPath $projectDir -buildWindows64Player (Join-Path $buildDir "MonstersAndSoldiers.exe") -logFile $buildLog | Out-Null
    if (Select-String -Path $buildLog -Pattern "Build Finished, Result: Success" -Quiet) {
        Write-Host "Build OK: $buildDir"
    } else {
        Write-Host "Build falhou. Log: $buildLog" -ForegroundColor Red
        exit 1
    }
}

exit $testExit
