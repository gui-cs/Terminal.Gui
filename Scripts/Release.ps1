# Script for doing a Release of Terminal.Gui
# 
# ⚠️ DEPRECATED: This manual script is kept for reference only.
# Please use the automated release workflow instead:
# https://github.com/gui-cs/Terminal.Gui/actions/workflows/release.yml
#
# For instructions, see CONTRIBUTING.md - Release Process section.
#
# For now just does Alpha
param(
    [Parameter(Mandatory=$true)]
    [int]$Version
)

$branch = "v2_release"
$tag = "$Version-prealpha"
$releaseMessage = "Release $tag"

try {
    Write-Host "Switching to branch $branch"
    git checkout $branch

    Write-Host "Pulling latest from upstream branch $branch"
    git pull upstream $branch

    Write-Host "Tagging release with tag $tag"
    git tag $tag -a -m $releaseMessage

    Write-Host "Creating empty commit with message $releaseMessage"
    git commit --allow-empty -m $releaseMessage

    Write-Host "Pushing changes to upstream"
    git push --atomic upstream $branch $tag
} catch {
    Write-Host "An error occurred: $_"
    exit 1
}

Write-Host "Script executed successfully"
