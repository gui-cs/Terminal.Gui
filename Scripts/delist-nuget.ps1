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
    
    # Extract the numeric part after the last dot (e.g., "2.0.0-develop.123" -> 123)
    if ($version -match '\.(\d+)$') {
        return [int]$matches[1]
    }
    return 0
}

# Process develop packages - keep only the most recent
$developPattern = "^2\.0\.0-develop\..*$"
$developVersions = $allVersions | Where-Object { $_ -match $developPattern }

if ($developVersions.Count -gt 1) {
    # Sort by version number and keep the most recent
    $sortedDevelopVersions = $developVersions | Sort-Object { Get-VersionSortKey $_ } -Descending
    $toKeep = $sortedDevelopVersions[0]
    $toUnlist = $sortedDevelopVersions | Select-Object -Skip 1
    
    Write-Host "Found $($developVersions.Count) develop versions. Keeping most recent: $toKeep"
    
    foreach ($version in $toUnlist) {
        Write-Host "Unlisting develop package: $packageId - $version"
        dotnet nuget delete $packageId $version --source $nugetSource --api-key $ApiKey --non-interactive
    }
} else {
    Write-Host "Found $($developVersions.Count) develop versions. Nothing to unlist."
}

# Process alpha packages - keep only the just-published one
$alphaPattern = "^2\.0\.0-alpha\..*$"
$alphaVersions = $allVersions | Where-Object { $_ -match $alphaPattern }

if ($alphaVersions.Count -gt 0) {
    # If a version was just published, keep only that one
    if ($JustPublishedVersion -ne "" -and $JustPublishedVersion -match $alphaPattern) {
        $toUnlist = $alphaVersions | Where-Object { $_ -ne $JustPublishedVersion }
        
        if ($toUnlist.Count -gt 0) {
            Write-Host "Found $($alphaVersions.Count) alpha versions. Keeping just-published: $JustPublishedVersion"
            
            foreach ($version in $toUnlist) {
                Write-Host "Unlisting alpha package: $packageId - $version"
                dotnet nuget delete $packageId $version --source $nugetSource --api-key $ApiKey --non-interactive
            }
        } else {
            Write-Host "Found $($alphaVersions.Count) alpha versions. Just-published version is the only one."
        }
    } else {
        # If no version was just published or it's not alpha, keep the most recent
        $sortedAlphaVersions = $alphaVersions | Sort-Object { Get-VersionSortKey $_ } -Descending
        $toKeep = $sortedAlphaVersions[0]
        
        if ($alphaVersions.Count -gt 1) {
            $toUnlist = $sortedAlphaVersions | Select-Object -Skip 1
            
            Write-Host "Found $($alphaVersions.Count) alpha versions. Keeping most recent: $toKeep"
            
            foreach ($version in $toUnlist) {
                Write-Host "Unlisting alpha package: $packageId - $version"
                dotnet nuget delete $packageId $version --source $nugetSource --api-key $ApiKey --non-interactive
            }
        } else {
            Write-Host "Found $($alphaVersions.Count) alpha versions. Nothing to unlist."
        }
    }
} else {
    Write-Host "No alpha versions found."
}

# Process beta packages - keep only the just-published one (for future use)
$betaPattern = "^2\.0\.0-beta\..*$"
$betaVersions = $allVersions | Where-Object { $_ -match $betaPattern }

if ($betaVersions.Count -gt 0) {
    # If a version was just published, keep only that one
    if ($JustPublishedVersion -ne "" -and $JustPublishedVersion -match $betaPattern) {
        $toUnlist = $betaVersions | Where-Object { $_ -ne $JustPublishedVersion }
        
        if ($toUnlist.Count -gt 0) {
            Write-Host "Found $($betaVersions.Count) beta versions. Keeping just-published: $JustPublishedVersion"
            
            foreach ($version in $toUnlist) {
                Write-Host "Unlisting beta package: $packageId - $version"
                dotnet nuget delete $packageId $version --source $nugetSource --api-key $ApiKey --non-interactive
            }
        } else {
            Write-Host "Found $($betaVersions.Count) beta versions. Just-published version is the only one."
        }
    } else {
        # If no version was just published or it's not beta, keep the most recent
        $sortedBetaVersions = $betaVersions | Sort-Object { Get-VersionSortKey $_ } -Descending
        $toKeep = $sortedBetaVersions[0]
        
        if ($betaVersions.Count -gt 1) {
            $toUnlist = $sortedBetaVersions | Select-Object -Skip 1
            
            Write-Host "Found $($betaVersions.Count) beta versions. Keeping most recent: $toKeep"
            
            foreach ($version in $toUnlist) {
                Write-Host "Unlisting beta package: $packageId - $version"
                dotnet nuget delete $packageId $version --source $nugetSource --api-key $ApiKey --non-interactive
            }
        } else {
            Write-Host "Found $($betaVersions.Count) beta versions. Nothing to unlist."
        }
    }
} else {
    Write-Host "No beta versions found."
}

Write-Host "Operation complete."
