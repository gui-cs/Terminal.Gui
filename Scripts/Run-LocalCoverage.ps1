# ------------------------------------------------------------
# Run-LocalCoverage.ps1
# Local-only: Unit + Parallel + Integration tests
# Quiet, merged coverage in /Test
# ------------------------------------------------------------

# 1. Define paths
$testDir    = Join-Path $PWD "Tests"
$covDir     = Join-Path $testDir "coverage"
$reportDir  = Join-Path $testDir "report"
$mergedFile = Join-Path $covDir "coverage.merged.cobertura.xml"

# 2. Clean old results
Write-Host "Cleaning old coverage files..."
Remove-Item -Recurse -Force $covDir, $reportDir -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $covDir, $reportDir -Force | Out-Null

dotnet build --configuration Debug

# ------------------------------------------------------------
# 3. Run UNIT TESTS (non-parallel)
# ------------------------------------------------------------
Write-Host "`nRunning UnitTests (quiet)..."
dotnet test Tests/UnitTests `
  --verbosity minimal `
  --collect:"XPlat Code Coverage" `
  --settings Tests/UnitTests/runsettings.coverage.xml `
  --blame-hang-timeout 10s

# ------------------------------------------------------------
# 4. Run UNIT TESTS (parallel)
# ------------------------------------------------------------
Write-Host "`nRunning UnitTestsParallelizable (quiet)..."
dotnet test Tests/UnitTestsParallelizable `
  --verbosity minimal `
  --collect:"XPlat Code Coverage" `
  --settings Tests/UnitTestsParallelizable/runsettings.xml `

# ------------------------------------------------------------
# 5. Run INTEGRATION TESTS
# ------------------------------------------------------------
Write-Host "`nRunning IntegrationTests (quiet)..."
dotnet test Tests/IntegrationTests `
  --verbosity minimal `
  --collect:"XPlat Code Coverage" `
  --settings Tests/IntegrationTests/runsettings.xml `

# ------------------------------------------------------------
# 6. Find ALL coverage files (from all 3 projects)
# ------------------------------------------------------------
$covFiles = Get-ChildItem -Path . -Recurse -Filter coverage.cobertura.xml -File -ErrorAction SilentlyContinue

if (-not $covFiles) {
    Write-Error "No coverage files found. Did all tests run?"
    exit 1
}

# ------------------------------------------------------------
# 7. Move to /Test/coverage
# ------------------------------------------------------------
Write-Host "Moving $($covFiles.Count) coverage file(s) to $covDir..."
foreach ($f in $covFiles) {
    Copy-Item $f.FullName -Destination $covDir -Force
}

# ------------------------------------------------------------
# 8. Merge into one file
# ------------------------------------------------------------
Write-Host "Merging coverage from all test projects..."
dotnet-coverage merge `
    -o $mergedFile `
    -f cobertura `
    "$covDir\*.cobertura.xml"

# ------------------------------------------------------------
# 9. Generate HTML + text report
# ------------------------------------------------------------
Write-Host "Generating final HTML report..."
reportgenerator `
    -reports:$mergedFile `
    -targetdir:$reportDir `
    -reporttypes:"Html;TextSummary"

# ------------------------------------------------------------
# 10. Show summary + open report
# ------------------------------------------------------------
Write-Host "`n=== Final Coverage Summary (Unit + Integration) ==="
Get-Content "$reportDir\Summary.txt"

$indexHtml = Join-Path $reportDir "index.html"
if (Test-Path $indexHtml) {
    Write-Host "Opening report in browser..."
    Start-Process $indexHtml
} else {
    Write-Warning "HTML report not found at $indexHtml"
}