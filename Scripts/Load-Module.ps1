<#
  .SYNOPSIS
  Loads the Terminal.Gui.PowerShell modules and pushes the current path to the location stack.
#>


$tgScriptsPath = Push-Location -PassThru
$tgModule = Import-Module "./Terminal.Gui.PowerShell.psd1" -PassThru

Set-PowerShellEnvironment