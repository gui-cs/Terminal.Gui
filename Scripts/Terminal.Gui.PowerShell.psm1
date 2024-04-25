<#
  .SYNOPSIS
  (Windows Only) Opens Visual Studio and loads Terminal.sln.
  .DESCRIPTION
  (Windows Only) Opens Visual Studio and loads Terminal.sln.
  .PARAMETER SolutionFilePath
  (Optional) If specified, the path to the solution file. Typically unnecessary to supply this parameter.
  .INPUTS
  None
  .OUTPUTS
  None
#>
Function Open-Solution {
  [CmdletBinding()]
  param(
    [Parameter(Mandatory=$false, HelpMessage="The path to the solution file to open.")]
    [Uri]$SolutionFilePath
  )
  
  if(!$IsWindows) {
    [string]$warningMessage = "The Open-Solution cmdlet is only supported on Windows.`n`
    Attempt to open file $SolutionFilePath with the system default handler?"
    
    Write-Warning $warningMessage -WarningAction Inquire
  }
  
  Invoke-Item $SolutionFilePath
  return
}

<#
  .SYNOPSIS
  (Windows Only) Closes Visual Studio processes with Terminal.sln loaded.
  .DESCRIPTION
  (Windows Only) Closes Visual Studio processes with Terminal.sln loaded by finding any VS processes launched with the solution file or with 'Terminal' in their main window titles.
  .INPUTS
  None
  .OUTPUTS
  None
#>
Function Close-Solution {
  $vsProcesses = Get-Process -Name devenv | Where-Object { ($_.CommandLine -Match ".*Terminal\.sln.*" -or $_.MainWindowTitle -Match "Terminal.*") }
  Stop-Process -InputObject $vsProcesses
  Remove-Variable vsProcesses
}

<#
  .SYNOPSIS
  Resets changes made by ConfigureEnvironment.pst to the current PowerShell environment.
  .DESCRIPTION
  Optional function to undo changes to the current session made by ConfigureEnvironment.ps1.
  Changes only affect the current session, so exiting will also "reset." 
  .PARAMETER Exit
  Switch parameter that, if specified, exits the current PowerShell environment.
  Does not bother doing any other operations, as none are necessary.
  .INPUTS
  None
  .OUTPUTS
  None
  .EXAMPLE
  Reset-PowerShellEnvironment
  To undo changes in the current session.
  .EXAMPLE
  Reset-PowerShellEnvironment -Exit
  To exit the current session. Same as simply using the Exit command.
#>
Function Reset-PowerShellEnvironment {
  param(
    [Parameter(Mandatory = $false)]
    [switch]$Exit
  )

  if($Exit) {
    [Environment]::Exit(0)
  }

  if(Get-Variable -Name NormalPrompt -Scope Global -ErrorAction SilentlyContinue){
    Set-Item Function:prompt $NormalPrompt
    Remove-Variable -Name NormalPrompt -Scope Global -Force
  }

  if(Get-Variable -Name OriginalPSModulePath -Scope Global -ErrorAction SilentlyContinue){
    $Env:PSModulePath = $OriginalPSModulePath
    Remove-Variable -Name OriginalPSModulePath -Scope Global -Force
  }

  Remove-Variable -Name PathVarSeparator -Scope Global -Force -ErrorAction SilentlyContinue
}

<#
  .SYNOPSIS
  Sets up a standard environment for other Terminal.Gui.PowerShell scripts and modules.
  .DESCRIPTION
  Configures environment variables and global variables for other Terminal.Gui.PowerShell scripts to use.
  Also modifies the prompt to indicate the session has been altered.
  Reset changes by exiting the session or by calling Reset-PowerShellEnvironment or ./ResetEnvironment.ps1.
#>

<#
  .SYNOPSIS
  Sets up a standard environment for other Terminal.Gui.PowerShell scripts and modules.
  .DESCRIPTION
  Configures environment variables and global variables for other Terminal.Gui.PowerShell scripts to use.
  Also modifies the prompt to indicate the session has been altered.
  Reset changes by exiting the session or by calling Reset-PowerShellEnvironment or ./ResetEnvironment.ps1.
#>
Function Set-PowerShellEnvironment {
  # Set a custom prompt to indicate we're in our modified environment.
  # Save the normal one first, though.
  New-Variable -Name NormalPrompt -Option ReadOnly -Scope Global -Value (Get-Item Function:prompt).ScriptBlock -ErrorAction SilentlyContinue
  Set-Item Function:prompt { "TGPS $($executionContext.SessionState.Path.CurrentLocation)$('>' * ($nestedPromptLevel + 1)) "; }

  # Save existing PSModulePath for optional reset later.
  # If it is already saved, do not overwrite, but continue anyway.
  New-Variable -Name OriginalPSModulePath -Visibility Public -Option ReadOnly -Scope Global -Value ($Env:PSModulePath) -ErrorAction SilentlyContinue
  Write-Debug -Message "`$OriginalPSModulePath is $OriginalPSModulePath"

  # Get platform-specific path variable entry separator. Continue if it's already set.
  New-Variable -Name PathVarSeparator -Visibility Public -Option ReadOnly,Constant -Scope Global -Value ";" -Description 'Separator character used in environment variables such as $Env:PSModulePath' -ErrorAction SilentlyContinue

  if(!$IsWindows) {
  $PathVarSeparator = ':'
  }
  Write-Debug -Message "`$PathVarSeparator is $PathVarSeparator"

  # Now make it constant.
  Set-Variable PathVarSeparator -Option Constant -ErrorAction SilentlyContinue

  # If Env:PSModulePath already has the current path, don't append it again.
  if($Env:PSModulePath -notlike "*$((Resolve-Path .).Path)*") {
      Write-Debug -Message "Appending $((Resolve-Path .).Path) to `$Env:PSModulePath"
      $env:PSModulePath = Join-String -Separator $PathVarSeparator -InputObject @( $env:PSModulePath, (Resolve-Path .).Path )
  }
  Write-Debug -Message "`$Env:PSModulePath is $Env:PSModulePath"
}

# This ensures the environment is reset when unloading the module.
# Without this, function:prompt will be undefined.
$MyInvocation.MyCommand.ScriptBlock.Module.OnRemove = { 
  Reset-PowerShellEnvironment
  Pop-Location
}