<#
  .SYNOPSIS
  Builds all analyzer projects in Debug and Release configurations.
  .DESCRIPTION
  Uses dotnet build to build all analyzer projects, with optional behavior changes via switch parameters.
  .PARAMETER AutoClose
  Automatically close running Visual Studio processes which have the Terminal.sln solution loaded, before taking any other actions.
  .PARAMETER AutoLaunch
  Automatically start a new Visual Studio process and load the solution after completion.
  .PARAMETER Force
  Carry out operations unconditionally and do not prompt for confirmation.
  .PARAMETER NoClean
  Do not delete the bin and obj folders before building the analyzers. Usually best not to use this, but can speed up the builds slightly.
  .PARAMETER Quiet
  Write less text output to the terminal.
  .INPUTS
  None
  .OUTPUTS
  None
#>
Function Build-Analyzers {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$false, HelpMessage="Automatically close running Visual Studio processes which have the Terminal.sln solution loaded, before taking any other actions.")]
    [switch]$AutoClose,
    [Parameter(Mandatory=$false, HelpMessage="Automatically start a new Visual Studio process and load the solution after completion.")]
    [switch]$AutoLaunch,
    [Parameter(Mandatory=$false, HelpMessage="Carry out operations unconditionally and do not prompt for confirmation.")]
    [switch]$Force,
    [Parameter(Mandatory=$false, HelpMessage="Do not delete the bin and obj folders before building the analyzers.")]
    [switch]$NoClean,
    [Parameter(Mandatory=$false, HelpMessage="Write less text output to the terminal.")]
    [switch]$Quiet
  )
  
  if($AutoClose) {
    if(!$Quiet) {
      Write-Host Closing Visual Studio processes
    }
    Close-Solution
  }

  if($Force){
    $response = 'Y'
  }
  elseif(!$Force && $NoClean){
    $response = ($r = Read-Host "Pre-build Terminal.Gui.InternalAnalyzers without removing old build artifacts? [Y/n]") ? $r : 'Y'
  }
  else{
    $response = ($r = Read-Host "Delete bin and obj folders for Terminal.Gui and Terminal.Gui.InternalAnalyzers and pre-build Terminal.Gui.InternalAnalyzers? [Y/n]") ? $r : 'Y'
  }

  if (($response -ne 'Y')) {
    Write-Host Took no action
    return
  }
  
  Push-Location $InternalAnalyzersProjectDirectory
  
  if(!$NoClean) {
    if(!$Quiet) {
      Write-Host Deleting bin and obj folders for Terminal.Gui
    }
    Remove-Item -Recurse -Force $TerminalGuiProjectDirectory/bin -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $TerminalGuiProjectDirectory/obj -ErrorAction SilentlyContinue

    if(!$Quiet) {
      Write-Host Deleting bin and obj folders for Terminal.Gui.InternalAnalyzers
    }
    Remove-Item -Recurse -Force $InternalAnalyzersProjectDirectory/bin -ErrorAction SilentlyContinue
    Remove-Item -Recurse -Force $InternalAnalyzersProjectDirectory/obj -ErrorAction SilentlyContinue
  }
  
  if(!$Quiet) {
    Write-Host Building analyzers in Debug configuration
  }
  dotnet build $InternalAnalyzersProjectFilePath --no-incremental --nologo --force --configuration Debug

  if(!$Quiet) {
    Write-Host Building analyzers in Release configuration
  }
  dotnet build $InternalAnalyzersProjectFilePath --no-incremental --nologo --force --configuration Release

  Pop-Location
  
  if(!$AutoLaunch) {
    Write-Host -ForegroundColor Green Finished. Restart Visual Studio for changes to take effect.
  } else {
    if(!$Quiet) {
      Write-Host -ForegroundColor Green Finished. Re-loading Terminal.sln.
    }
    Open-Solution
  }

  return
}
