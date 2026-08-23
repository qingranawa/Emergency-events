$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$sourceTests = $PSScriptRoot
$sourceRoot = Split-Path -Parent $sourceTests
$tempRoot = Join-Path ([IO.Path]::GetTempPath()) "emergency-events-task2-tdd-$([Guid]::NewGuid().ToString('N'))"
$stagedTests = Join-Path $tempRoot 'Evaluation.Tests'
$stagedEvaluation = Join-Path $tempRoot 'Evaluation'
$stagedRoundCore = Join-Path $tempRoot 'RoundCore'
$stagedProject = Join-Path $stagedTests 'Evaluation.Tests.csproj'
$scriptExitCode = 1

function Invoke-DotnetRun {
    param(
        [Parameter(Mandatory = $true)]
        [string]$ProjectPath
    )

    $outputLines = @(& dotnet run --project $ProjectPath -p:NuGetAudit=false 2>&1)
    $exitCode = $LASTEXITCODE
    [pscustomobject]@{
        ExitCode = $exitCode
        Output = $outputLines -join [Environment]::NewLine
    }
}

try {
    New-Item -ItemType Directory -Path $stagedTests, $stagedEvaluation, $stagedRoundCore -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $sourceTests 'Evaluation.Tests.csproj') -Destination $stagedTests
    Copy-Item -LiteralPath (Join-Path $sourceTests 'Program.cs') -Destination $stagedTests
    Copy-Item -LiteralPath (Join-Path $sourceRoot 'RoundCore\PopulationTier.cs') -Destination $stagedRoundCore

    $redStartedAt = [DateTimeOffset]::Now
    $red = Invoke-DotnetRun -ProjectPath $stagedProject
    $redFinishedAt = [DateTimeOffset]::Now
    $redHasMissingModelError = $red.Output -match '(?i)CS0246' -and $red.Output -match '(?i)RoundSnapshot'
    $redPassed = $red.ExitCode -eq 1 -and $redHasMissingModelError

    Write-Output 'TDD_RED_COMMAND=dotnet run --project <isolated-temp>\Evaluation.Tests\Evaluation.Tests.csproj -p:NuGetAudit=false'
    Write-Output "TDD_RED_STARTED_AT=$($redStartedAt.ToString('o'))"
    Write-Output "TDD_RED_FINISHED_AT=$($redFinishedAt.ToString('o'))"
    Write-Output "TDD_RED_EXIT_CODE=$($red.ExitCode)"
    Write-Output "TDD_RED_EXPECTED_ERROR_FOUND=$redHasMissingModelError"
    Write-Output 'TDD_RED_OUTPUT_BEGIN'
    Write-Output $red.Output.Trim()
    Write-Output 'TDD_RED_OUTPUT_END'

    if (-not $redPassed) {
        throw "RED 重演断言失败：需要退出码 1 且输出包含 CS0246 与 RoundSnapshot。"
    }

    Get-ChildItem -LiteralPath (Join-Path $sourceRoot 'Evaluation') -Filter '*.cs' -File |
        Copy-Item -Destination $stagedEvaluation

    $greenStartedAt = [DateTimeOffset]::Now
    $green = Invoke-DotnetRun -ProjectPath $stagedProject
    $greenFinishedAt = [DateTimeOffset]::Now
    $greenHasExpectedSummary = $green.Output -match '(?m)^Total:\s*4\s*$' -and
        $green.Output -match '(?m)^Failed:\s*0\s*$'
    $greenPassed = $green.ExitCode -eq 0 -and $greenHasExpectedSummary

    Write-Output 'TDD_GREEN_COMMAND=dotnet run --project <same-isolated-temp>\Evaluation.Tests\Evaluation.Tests.csproj -p:NuGetAudit=false'
    Write-Output "TDD_GREEN_STARTED_AT=$($greenStartedAt.ToString('o'))"
    Write-Output "TDD_GREEN_FINISHED_AT=$($greenFinishedAt.ToString('o'))"
    Write-Output "TDD_GREEN_EXIT_CODE=$($green.ExitCode)"
    Write-Output "TDD_GREEN_EXPECTED_SUMMARY_FOUND=$greenHasExpectedSummary"
    Write-Output 'TDD_GREEN_OUTPUT_BEGIN'
    Write-Output $green.Output.Trim()
    Write-Output 'TDD_GREEN_OUTPUT_END'

    if (-not $greenPassed) {
        throw "GREEN 重演断言失败：需要退出码 0、Total: 4 和 Failed: 0。"
    }

    $scriptExitCode = 0
    Write-Output 'TDD_REPLAY_RESULT=PASS'
}
catch {
    Write-Output 'TDD_REPLAY_RESULT=FAIL'
    Write-Output "TDD_REPLAY_ERROR=$($_.Exception.Message)"
    $scriptExitCode = 1
}
finally {
    if ($null -ne $tempRoot -and (Test-Path -LiteralPath $tempRoot)) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}

exit $scriptExitCode
