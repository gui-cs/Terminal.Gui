# Hero GIF Guidance

For recording Terminal.Gui app/scenario GIFs, use:

- [`./README.md`](./README.md) — Full recording workflow with tuirec

## Quick Reference

```powershell
# Install tuirec (one-time)
go install github.com/gui-cs/tuirec/cmd/tuirec@latest

# Build ScenarioRunner (before any recording)
dotnet build Examples/ScenarioRunner/ScenarioRunner.csproj -c Release

# Record a scenario
$binary = "./Examples/ScenarioRunner/bin/Release/net10.0/ScenarioRunner.exe"
$ks = 'wait:1000,<keystrokes>,Escape'
tuirec record --binary $binary --args "run,<Scenario Name>" --name <id> `
    --keystrokes $ks --startup-delay 2000 --drain 2000 --cols 120 --rows 30 --open
```

See `README.md` (this directory) for complete guidance including keystroke syntax,
PowerShell quoting rules, and the `--kitty-keyboard` decision tree.

## File Placement

- **Scenario GIFs** go alongside the scenario `.cs` file:
  `Examples/UICatalog/Scenarios/<ScenarioDir>/<ScenarioName>.gif`
- **View GIFs** go alongside the View's `.cs` file:
  `Terminal.Gui/Views/<ViewName>.gif`
