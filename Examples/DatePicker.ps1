using namespace Terminal.Gui.App
using namespace Terminal.Gui.ViewBase
using namespace Terminal.Gui.Views
# DatePicker.ps1 - Demonstrates Prompt API with DatePicker
#
# Usage:
#   ./DatePicker.ps1                    # Uses today's date
#   ./DatePicker.ps1 '9/10/1966'        # Uses specified date
#
# Prerequisites:
#   dotnet build ..\Terminal.Gui -c Debug /p:CopyLocalLockFileAssemblies=true
#
# Returns:
#   Selected date (yyyy-MM-dd format) if user accepts
#   Nothing if user cancels

param(
    [string]$InitialDate = ""
)


$dllFolder = "Terminal.Gui\bin\Debug\net10.0"

# Load all Terminal.Gui DLLs
Get-ChildItem $dllFolder -Filter *.dll | ForEach-Object {
    Add-Type -Path $_.FullName -ErrorAction SilentlyContinue
}

# Parse initial date or use today
$date = if ($InitialDate) {
    [DateTime]::Parse($InitialDate)
} else {
    [DateTime]::Now
}

$app = [Application]::Create().Init()
$app.Init()

try {
    $datePicker = [DatePicker]::new($date)

    # Use the bool-returning overload (simpler for PowerShell)
    # The generic Func<>-based overload requires complex delegate creation
    $accepted = [Prompt]::Show($app, "Select Date", $datePicker)

    if ($accepted) {
        Write-Output $datePicker.Date.ToString("yyyy-MM-dd")
    }
}
finally {
    if ($datePicker) { $datePicker.Dispose() }
    $app.Dispose()
}
