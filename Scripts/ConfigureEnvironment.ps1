<#
  .SYNOPSIS
  Sets up a standard environment for other Terminal.Gui.PowerShell scripts and modules.
  .DESCRIPTION
  Configures environment variables and global variables for other Terminal.Gui.PowerShell scripts to use.
  Also modifies the prompt to indicate the session has been altered.
  Reset changes by exiting the session or by calling Reset-PowerShellEnvironment or ./ResetEnvironment.ps1.
#>


Set-Environment