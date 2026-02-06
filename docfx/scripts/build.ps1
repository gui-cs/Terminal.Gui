# Builds the Terminal.gui API documentation using docfx

# Get the script directory and derive the docfx directory from it
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$docfxDir = Split-Path -Parent $scriptDir
$prevPwd = $PWD
Set-Location $docfxDir

try {
    Write-Host "Working directory: $(Get-Location)"

    # Generate source index for AI agents (Vercel-style file path index)
    Write-Host "Generating source index..." -ForegroundColor Cyan
    & "$scriptDir/Generate-SourceIndex.ps1"

    dotnet tool update -g docfx

    # Force delete metadata
    Remove-Item ./api -Recurse -Force -ErrorAction SilentlyContinue

    # Force delete generated _site
    Remove-Item ./_site -Recurse -Force -ErrorAction SilentlyContinue

    $env:DOCFX_SOURCE_BRANCH_NAME="v2_develop"

    docfx --serve
}
finally {
    # Restore the previous location
    $prevPwd | Set-Location
}

