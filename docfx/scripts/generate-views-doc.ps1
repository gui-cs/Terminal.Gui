# Script to generate views.md from API documentation
param(
    [string]$ApiPath = "api",
    [string]$OutputPath = "docs/views.md"
)

# Ensure we're in the correct directory (docfx root)
$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$rootPath = Split-Path -Parent $scriptPath
Set-Location $rootPath

Write-Host "Working directory: $(Get-Location)"
Write-Host "Looking for view files in: $ApiPath"

# Get all .yml files in the API directory that are Views
$viewFiles = Get-ChildItem -Path $ApiPath -Filter "Terminal.Gui.Views.*.yml"
Write-Host "Found $($viewFiles.Count) view files"

# Start building the markdown content
$content = @"
# Views

*Terminal.Gui* provides a rich set of views and controls for building terminal user interfaces:

| View | Description |
|------|-------------|
"@

# Process each view file
$views = @()
foreach ($file in $viewFiles) {
    try {
        $yml = Get-Content $file.FullName -Raw | ConvertFrom-Yaml
        
        # Check if this is actually a View type
        $isView = $false
        if ($yml.items[0].inheritance) {
            $isView = $yml.items[0].inheritance -contains "Terminal.Gui.ViewBase.View"
        }
        
        if (-not $isView) {
            continue
        }
        
        # Extract the view name and description
        $name = $file.BaseName -replace "^Terminal\.Gui\.Views\.", ""
        
        # Handle generic types
        if ($name -match "(.+)-(\d+)$") {
            $name = "$($matches[1])\<T\>"
        }
        
        $description = $yml.items[0].summary
        
        # Clean up the description
        $description = $description -replace "`r`n", " "  # Replace newlines with spaces
        $description = $description -replace "\s+", " "    # Replace multiple spaces with single space
        $description = $description.Trim()                # Trim leading/trailing whitespace
        
        # Remove duplicate content (only for repeated phrases, not characters)
        $description = $description -replace '([^a-zA-Z0-9]+)\1+', '$1'
        
        # Clean up HTML tags
        $description = $description -replace '<p>|</p>', ''  # Remove paragraph tags
        $description = $description -replace '<a href="[^"]+">([^<]+)</a>', '$1'  # Remove links but keep text
        
        # Convert ALL xref tags to markdown links
        $description = $description -replace '<xref href="([^"]+)"[^>]*>([^<]+)</xref>', '[$2](~/api/$1.yml)'
        $description = $description -replace '<see cref="([^"]+)"/>', '[$1](~/api/$1.yml)'
        $description = $description -replace '<c>([^<]+)</c>', '`$1`'
        
        # Convert code tags to backticks
        $description = $description -replace '<code>([^<]*)</code>', '`$1`'
        
        # Fix any remaining xref tags
        $description = $description -replace '<xref href="([^"]+)"[^>]*></xref>', '[$1](~/api/$1.yml)'
        
        # Extract just the class name from full type names
        $description = $description -replace '\[Terminal\.Gui\.Views\.([^\]]+)\]', '[$1]'
        $description = $description -replace '\[Terminal\.Gui\.ViewBase\.([^\]]+)\]', '[$1]'
        $description = $description -replace '\[Terminal\.Gui\.Drawing\.([^\]]+)\]', '[$1]'
        $description = $description -replace '\[Terminal\.Gui\.Input\.([^\]]+)\]', '[$1]'
        $description = $description -replace '\[System\.([^\]]+)\]', '[$1]'
        
        Write-Host "Found view: $name"
        $views += "| [$name](~/api/$($file.BaseName).yml) | $description |"
    }
    catch {
        Write-Host "  Error processing $($file.Name): $_" -ForegroundColor Red
        Write-Host "  YAML content:"
        Write-Host (Get-Content $file.FullName -Raw)
    }
}

Write-Host "Sorting views..."
# Sort the views alphabetically
$views = $views | Sort-Object

Write-Host "Generating markdown..."
# Add the views to the content
$content += "`n" + ($views -join "`n")

Write-Host "Writing to $OutputPath..."
# Write the content to the output file
$content | Set-Content -Path $OutputPath -NoNewline

Write-Host "Generated $OutputPath successfully" -ForegroundColor Green