using namespace Terminal.Gui.App        
using namespace Terminal.Gui.ViewBase
using namespace Terminal.Gui.Views

$dllFolder = "..\Terminal.Gui\bin\Debug\net8.0"

# For this to work all dependent DLLs need to be in the $dllFolder folder
# Do this first:
#   dotnet build -c Debug /p:CopyLocalLockFileAssemblies=true

Get-ChildItem $dllFolder -Filter *.dll | ForEach-Object {
    Add-Type -Path $_.FullName -ErrorAction SilentlyContinue
}

$app = [Application]::Create()

$app.Init()

$win = [Window]@{
    Title  = "Terminal.Gui in Powershell"
    Width  = [Dim]::Fill()
    Height = [Dim]::Fill()
}

$lbl = [Label]@{
    Text = "Hello from PowerShell + Terminal.Gui!`nPress ESC to quit"
    X    = [Pos]::Center()
    Y    = [Pos]::Center()
}
$win.Add($lbl)

$app.Run($win)

$win.Dispose()
$app.Dispose()
