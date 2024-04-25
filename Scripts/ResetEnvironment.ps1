<#
  .SYNOPSIS
  Resets changes made by ConfigureEnvironment.pst to the current PowerShell environment.
  .DESCRIPTION
  Optional script to undo changes to the current session made by ConfigureEnvironment.ps1.
  Changes only affect the current session, so exiting will also "reset." 
  .PARAMETER Exit
  Switch parameter that, if specified, exits the current PowerShell environment.
  Does not bother doing any other operations, as none are necessary.
  .INPUTS
  None
  .OUTPUTS
  None
  .EXAMPLE
  .\ResetEnvironment.ps1
  To run the script to undo changes in the current session.
  .EXAMPLE
  .\ResetEnvironment.ps1 -Exit
  To exit the current session. Same as simply using the Exit command.
#>


# The two blank lines above must be preserved.
Import-Module ./Terminal.Gui.PowerShell.psd1

if($args -contains "-Exit"){
  [Environment]::Exit(0)
} else {
  Reset-PowerShellEnvironment
}

Remove-Module Terminal.Gui.PowerShell