# PowerShell script to unlist NuGet packages using dotnet CLI
# This script delists old develop and alpha packages while keeping the most recent ones
param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,
    
    [Parameter(Mandatory=$false)]
    [string]$JustPublishedVersion = ""
)

$packageId = "terminal.gui"  # Ensure this is the correct package name (case-sensitive)
$nugetSource = "https://api.nuget.org/v3/index.json"

# Fetch package versions from NuGet API
$nugetApiUrl = "https://api.nuget.org/v3-flatcontainer/$packageId/index.json"
Write-Host "Fetching package versions for '$packageId'..."

try {
    $versionsResponse = Invoke-RestMethod -Uri $nugetApiUrl
    $allVersions = $versionsResponse.versions
} catch {
    Write-Host "Error fetching package versions: $_"
    exit 1
}

# Function to parse version and extract numeric parts for comparison
function Get-VersionSortKey {
    param([string]$version)
    
    # Extract the numeric part after the last dot for prerelease versions
    # E.g., "2.0.0-develop.123" -> 123, "2.0.0-alpha.5" -> 5
    if ($version -match '[\-\.](\d+)$') {
        return [int]$matches[1]
    }
    return 0
}

# Function to process package versions with a specific pattern
function Process-PackageVersions {
    param(
        [string]$Pattern,
        [string]$PackageType,
        [array]$AllVersions,
        [string]$JustPublished = ""
    )
    
    $matchingVersions = $AllVersions | Where-Object { $_ -match $Pattern }
    
    if ($matchingVersions.Count -eq 0) {
        Write-Host "No $PackageType versions found."
        return
    }
    
    # Determine which version to keep
    $toKeep = $null
    $toUnlist = @()
    
    if ($JustPublished -ne "" -and $JustPublished -match $Pattern) {
        # Keep the just-published version
        $toKeep = $JustPublished
        $toUnlist = $matchingVersions | Where-Object { $_ -ne $JustPublished }
        
        if ($toUnlist.Count -gt 0) {
            Write-Host "Found $($matchingVersions.Count) $PackageType versions. Keeping just-published: $toKeep"
        } else {
            Write-Host "Found $($matchingVersions.Count) $PackageType versions. Just-published version is the only one."
            return
        }
    } else {
        # Keep the most recent version
        if ($matchingVersions.Count -eq 1) {
            Write-Host "Found 1 $PackageType version. Nothing to unlist."
            return
        }
        
        $sortedVersions = $matchingVersions | Sort-Object { Get-VersionSortKey $_ } -Descending
        $toKeep = $sortedVersions[0]
        $toUnlist = $sortedVersions | Select-Object -Skip 1
        
        Write-Host "Found $($matchingVersions.Count) $PackageType versions. Keeping most recent: $toKeep"
    }
    
    # Delist versions
    foreach ($version in $toUnlist) {
        Write-Host "Unlisting $PackageType package: $packageId - $version"
        dotnet nuget delete $packageId $version --source $nugetSource --api-key $ApiKey --non-interactive
    }
}

# Process develop packages - keep only the most recent
Process-PackageVersions -Pattern "^2\.0\.0-develop\..*$" -PackageType "develop" -AllVersions $allVersions

# Process alpha packages - keep only the just-published one or most recent
Process-PackageVersions -Pattern "^2\.0\.0-alpha\..*$" -PackageType "alpha" -AllVersions $allVersions -JustPublished $JustPublishedVersion

# Process beta packages - keep only the just-published one or most recent (for future use)
Process-PackageVersions -Pattern "^2\.0\.0-beta\..*$" -PackageType "beta" -AllVersions $allVersions -JustPublished $JustPublishedVersion

Write-Host "Operation complete."
