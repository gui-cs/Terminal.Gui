Function Update-Analyzers {
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
  
  New-Variable -Name solutionRoot -Visibility Public -Value (Resolve-Path ..)
  Push-Location $solutionRoot
  New-Variable -Name solutionFile -Visibility Public -Value (Resolve-Path ./Terminal.sln)
  $mainProjectRoot = Resolve-Path ./Terminal.Gui
  $mainProjectFile = Join-Path $mainProjectRoot Terminal.Gui.csproj
  $analyzersRoot = Resolve-Path ./Analyzers
  $internalAnalyzersProjectRoot = Join-Path $analyzersRoot Terminal.Gui.Analyzers.Internal
  $internalAnalyzersProjectFile = Join-Path $internalAnalyzersProjectRoot Terminal.Gui.Analyzers.Internal.csproj
  
  if(!$NoClean) {
    if(!$Quiet) {
      Write-Host Deleting bin and obj folders for Terminal.Gui
    }
    if(Test-Path $mainProjectRoot/bin) {
      Remove-Item -Recurse -Force $mainProjectRoot/bin
      Remove-Item -Recurse -Force $mainProjectRoot/obj
    }

    if(!$Quiet) {
      Write-Host Deleting bin and obj folders for Terminal.Gui.InternalAnalyzers
    }
    if(Test-Path $internalAnalyzersProjectRoot/bin) {
      Remove-Item -Recurse -Force $internalAnalyzersProjectRoot/bin
      Remove-Item -Recurse -Force $internalAnalyzersProjectRoot/obj
    }
  }
  
  if(!$Quiet) {
    Write-Host Building analyzers in Debug configuration
  }
  dotnet build $internalAnalyzersProjectFile --no-incremental --nologo --force --configuration Debug

  if(!$Quiet) {
    Write-Host Building analyzers in Release configuration
  }
  dotnet build $internalAnalyzersProjectFile --no-incremental --nologo --force --configuration Release

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

Function Open-Solution {
  Invoke-Item $solutionFile
  return  
}

Function Close-Solution {
  $vsProcesses = Get-Process -Name devenv | Where-Object { ($_.CommandLine -Match ".*Terminal\.sln.*" -or $_.MainWindowTitle -Match "Terminal.*") }
  Stop-Process -InputObject $vsProcesses
  Remove-Variable vsProcesses
}

Export-ModuleMember -Function Update-Analyzers
Export-ModuleMember -Function Open-Solution
Export-ModuleMember -Function Close-Solution
