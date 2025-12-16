$apiKey = ""  # Replace with your actual API key
# Unlist all packages matching "2.0.0-v2-develop.*"
# PowerShell script to unlist NuGet packages using dotnet CLI
$packageId = "terminal.gui"  # Ensure this is the correct package name (case-sensitive)
$packagePattern = "^2\.0\.0-develop\..*$"  # Regex pattern for filtering versions
$nugetSource = "https://api.nuget.org/v3/index.json"

# Fetch package versions from NuGet API
$nugetApiUrl = "https://api.nuget.org/v3-flatcontainer/$packageId/index.json"
Write-Host "Fetching package versions for '$packageId'..."

try {
    $versionsResponse = Invoke-RestMethod -Uri $nugetApiUrl
    $matchingVersions = $versionsResponse.versions | Where-Object { $_ -match $packagePattern }
} catch {
    Write-Host "Error fetching package versions: $_"
    exit 1
}

if ($matchingVersions.Count -eq 0) {
    Write-Host "No matching packages found for '$packageId' with pattern '$packagePattern'."
    exit 0
}

# Unlist each matching package version
foreach ($version in $matchingVersions) {
    Write-Host "Unlisting package: $packageId - $version"
    dotnet nuget delete $packageId $version --source $nugetSource --api-key $apiKey --non-interactive
}

Write-Host "Operation complete."
