# Code Cleanup Agent for Terminal.Gui
# WARNING: ReSharper cleanupcode may process entire solution regardless of --include parameter
# Recommend running ReSharper cleanup manually in IDE or running on whole solution once

param (
    [Parameter(Mandatory = $true)]
    [string[]]$Files,

    [Parameter(Mandatory = $false)]
    [switch]$SkipReSharper,

    [Parameter(Mandatory = $false)]
    [switch]$SkipTests,

    [Parameter(Mandatory = $false)]
    [switch]$SkipPartialSplit
)

$ErrorActionPreference = "Stop"
$RepoRoot = Split-Path $PSScriptRoot -Parent
$StartTime = Get-Date

Write-Host @"
===============================================
  Code Cleanup Agent for Terminal.Gui
===============================================
  Files to process: $($Files.Count)
  Skip ReSharper: $SkipReSharper
  Skip tests: $SkipTests
  Skip partial split: $SkipPartialSplit
===============================================
"@ -ForegroundColor Cyan

# Statistics
$Stats = @{
    TotalFiles = $Files.Count
    SuccessFiles = @()
    FailedFiles = @()
    FilesSplit = @()
    NullableAdded = 0
    CWPTodosAdded = 0
}

function Write-Status {
    param([string]$Message, [string]$Color = "Cyan")
    Write-Host "  [$((Get-Date).ToString('HH:mm:ss'))] $Message" -ForegroundColor $Color
}

function Test-NeedsPartialSplit {
    param([string]$FilePath)

    if ($SkipPartialSplit) { return $false }

    $lineCount = (Get-Content $FilePath).Count
    return $lineCount -gt 1000
}

function Invoke-PartialSplit {
    param([string]$FilePath)

    $lineCount = (Get-Content $FilePath).Count
    Write-Status "File has $lineCount lines, splitting into partials..." "Yellow"

    dotnet run --project "$RepoRoot\Scripts\PartialSplitter" -- $FilePath 2>&1 | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Partial split failed" "Red"
        return $false
    }

    return $true
}

function Invoke-ReSharperCleanup {
    if ($SkipReSharper) {
        Write-Status "Skipping ReSharper (--SkipReSharper flag)" "Yellow"
        return $true
    }

    Write-Status "Running ReSharper Full Cleanup on entire solution..." "Yellow"
    Write-Status "WARNING: This will format the entire codebase!" "Red"

    # ReSharper cleanupcode doesn't respect --include properly, so we run on whole solution
    # This is a known limitation - consider running manually in IDE instead
    jb cleanupcode "$RepoRoot\Terminal.sln" `
        --profile="Full Cleanup" `
        --no-build `
        --verbosity=ERROR 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Status "ReSharper cleanup failed" "Red"
        return $false
    }

    return $true
}

function Invoke-BackingFieldReorder {
    param([string]$FilePath)

    Write-Status "Reordering backing fields..."

    dotnet run --project "$RepoRoot\Scripts\BackingFieldReorderer" -- $FilePath 2>&1 | Out-Null

    return $LASTEXITCODE -eq 0
}

function Invoke-EnableNullable {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw

    # Check if already has nullable enable
    if ($content -match '#nullable\s+enable') {
        return $false
    }

    # Check if project has nullable enabled globally
    $projectFile = Get-Item "$RepoRoot\Terminal.Gui\Terminal.Gui.csproj" -ErrorAction SilentlyContinue
    if ($projectFile) {
        $projectContent = Get-Content $projectFile.FullName -Raw
        if ($projectContent -match '<Nullable>enable</Nullable>') {
            # Project has nullable enabled globally - directive not needed
            return $false
        }
    }

    # Check if file has nullable disable (don't override it)
    if ($content -match '#nullable\s+disable') {
        return $false
    }

    Write-Status "Adding #nullable enable directive..."

    $lines = $content -split "`r?`n"
    $insertIndex = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^using\s+') {
            $insertIndex = $i + 1
        }
        if ($lines[$i] -match '^namespace\s+') {
            $insertIndex = $i + 1
            break
        }
    }

    $newLines = @()
    $newLines += $lines[0..$insertIndex]
    $newLines += ""
    $newLines += "#nullable enable"
    if ($insertIndex + 1 -lt $lines.Count) {
        $newLines += $lines[($insertIndex + 1)..($lines.Count - 1)]
    }

    $newContent = $newLines -join "`r`n"
    Set-Content $FilePath -Value $newContent -NoNewline

    return $true
}

function Add-CWPTodoComments {
    param([string]$FilePath)

    $content = Get-Content $FilePath -Raw
    $lines = $content -split "`r?`n"
    $modified = $false
    $todosAdded = 0

    for ($i = 0; $i -lt $lines.Count; $i++) {
        $line = $lines[$i]

        # Detect virtual methods that could use CWP
        if ($line -match '^\s*(public|protected|internal)\s+virtual\s+\w+\s+On\w+\s*\(') {
            # Check if TODO comment already exists in previous lines
            $hasTodo = $false
            for ($j = [Math]::Max(0, $i - 3); $j -lt $i; $j++) {
                if ($lines[$j] -match 'TODO.*CWP') {
                    $hasTodo = $true
                    break
                }
            }

            if (-not $hasTodo) {
                $indent = ($line -replace '^(\s*).*', '$1')
                $lines[$i] = "$indent// TODO: Consider refactoring to use Cancellable Work Pattern (CWP)`r`n$line"
                $modified = $true
                $todosAdded++
            }
        }

        # Detect event declarations that might need CWP
        if ($line -match '^\s*public\s+event\s+\w+') {
            $eventName = if ($line -match 'event\s+\w+\s+(\w+)') { $Matches[1] } else { $null }
            if ($eventName) {
                $raiseMethod = "Raise$eventName"
                $hasRaiseMethod = $content -match "function\s+$raiseMethod\s*\(|void\s+$raiseMethod\s*\("

                if (-not $hasRaiseMethod) {
                    $hasTodo = $false
                    for ($j = [Math]::Max(0, $i - 3); $j -lt $i; $j++) {
                        if ($lines[$j] -match 'TODO.*CWP') {
                            $hasTodo = $true
                            break
                        }
                    }

                    if (-not $hasTodo) {
                        $indent = ($line -replace '^(\s*).*', '$1')
                        $lines[$i] = "$indent// TODO: Consider adding Raise$eventName method using CWP pattern`r`n$line"
                        $modified = $true
                        $todosAdded++
                    }
                }
            }
        }
    }

    if ($modified) {
        Write-Status "Added $todosAdded CWP TODO comments" "Green"
        $newContent = $lines -join "`r`n"
        Set-Content $FilePath -Value $newContent -NoNewline
        return $todosAdded
    }

    return 0
}

function Invoke-RebuildSolution {
    Write-Status "Rebuilding solution..."

    dotnet build "$RepoRoot\Terminal.sln" --no-restore --configuration Debug --verbosity quiet 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Build failed" "Red"
        return $false
    }

    return $true
}

function Invoke-RunTests {
    if ($SkipTests) {
        Write-Status "Skipping tests (--SkipTests flag)" "Yellow"
        return $true
    }

    Write-Status "Running tests (UnitTestsParallelizable)..."

    dotnet test "$RepoRoot\Tests\UnitTestsParallelizable" `
        --no-build `
        --verbosity minimal `
        --logger "console;verbosity=minimal" 2>&1 | Out-Null

    if ($LASTEXITCODE -ne 0) {
        Write-Status "Tests failed" "Red"
        return $false
    }

    Write-Status "Tests passed" "Green"
    return $true
}

function Invoke-RollbackFile {
    param([string]$FilePath)

    Write-Status "Rolling back changes..." "Yellow"
    git checkout HEAD -- $FilePath 2>&1 | Out-Null

    return $LASTEXITCODE -eq 0
}

function Process-CleanupFile {
    param([string]$FilePath)

    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "Processing: $FilePath" -ForegroundColor White
    Write-Host "========================================" -ForegroundColor Cyan

    $wasSplit = $false

    try {
        # Step 1: Check file length & split if needed
        if (Test-NeedsPartialSplit -FilePath $FilePath) {
            $wasSplit = Invoke-PartialSplit -FilePath $FilePath

            if ($wasSplit) {
                $Stats.FilesSplit += $FilePath
                Write-Status "File split successfully." "Green"

                # Get all partial files created
                $directory = Split-Path $FilePath -Parent
                $baseName = [System.IO.Path]::GetFileNameWithoutExtension($FilePath)
                $partialFiles = Get-ChildItem "$directory\$baseName*.cs" -Exclude "*.backup"

                Write-Status "Created $($partialFiles.Count) partial files. Process each individually." "Cyan"
                return $true
            }
        }

        # Step 2: Run ReSharper cleanup (warning: processes entire solution)
        if (-not (Invoke-ReSharperCleanup)) {
            return $false
        }

        # Step 3: Fix backing field placement
        if (-not (Invoke-BackingFieldReorder -FilePath $FilePath)) {
            Write-Status "Backing field reordering failed" "Red"
            return $false
        }

        # Step 4: Ensure #nullable enable (if needed)
        $nullableAdded = Invoke-EnableNullable -FilePath $FilePath
        if ($nullableAdded) {
            $Stats.NullableAdded++
        }

        # Step 5: Add CWP TODO comments
        $cwpTodos = Add-CWPTodoComments -FilePath $FilePath
        $Stats.CWPTodosAdded += $cwpTodos

        # Step 6: Rebuild solution
        if (-not (Invoke-RebuildSolution)) {
            Invoke-RollbackFile -FilePath $FilePath
            return $false
        }

        # Step 7: Run tests
        if (-not (Invoke-RunTests)) {
            if (-not $wasSplit) {
                Invoke-RollbackFile -FilePath $FilePath
                return $false
            } else {
                Write-Status "Tests failed after split, keeping partials for manual review" "Yellow"
            }
        }

        Write-Status "✓ Success!" "Green"
        return $true
    }
    catch {
        Write-Host "ERROR: $_" -ForegroundColor Red
        Write-Host $_.ScriptStackTrace -ForegroundColor Red
        Invoke-RollbackFile -FilePath $FilePath
        return $false
    }
}

# Main Execution
foreach ($file in $Files) {
    $filePath = if ([System.IO.Path]::IsPathRooted($file)) {
        $file
    } else {
        Join-Path $RepoRoot $file
    }

    if (-not (Test-Path $filePath)) {
        Write-Host "File not found: $filePath" -ForegroundColor Red
        $Stats.FailedFiles += @{ Path = $filePath; Reason = "File not found" }
        continue
    }

    $success = Process-CleanupFile -FilePath $filePath

    if ($success) {
        $Stats.SuccessFiles += $filePath
    } else {
        $Stats.FailedFiles += @{ Path = $filePath; Reason = "Cleanup failed" }
    }
}

# Summary Report
$endTime = Get-Date
$duration = $endTime - $StartTime

Write-Host @"

========== CODE CLEANUP REPORT ==========
Session Start: $($StartTime.ToString('yyyy-MM-dd HH:mm:ss'))
Session End:   $($endTime.ToString('yyyy-MM-dd HH:mm:ss'))
Total Duration: $($duration.ToString('hh\:mm\:ss'))

FILES PROCESSED: $($Stats.TotalFiles)
  ✓ Success: $($Stats.SuccessFiles.Count)
  ✗ Failed:  $($Stats.FailedFiles.Count)

FILES SPLIT INTO PARTIALS: $($Stats.FilesSplit.Count)

NULLABLE DIRECTIVES ADDED: $($Stats.NullableAdded)
CWP TODO COMMENTS ADDED: $($Stats.CWPTodosAdded)

FAILED FILES:
$(if ($Stats.FailedFiles.Count -gt 0) { $Stats.FailedFiles | ForEach-Object { "  - $($_.Path): $($_.Reason)" } } else { "  None" })

=========================================
"@ -ForegroundColor Cyan

exit $($Stats.FailedFiles.Count -gt 0 ? 1 : 0)
