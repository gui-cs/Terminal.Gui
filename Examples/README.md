# Terminal.Gui Examples

This repository now keeps only:

- `UICatalog` - the main demo app
- `ScenarioRunner` - scenario automation tool

All other examples were moved to the [gui-cs/Examples](https://github.com/gui-cs/Examples) repository.

## Building Examples Against Local Source

The [gui-cs/Examples](https://github.com/gui-cs/Examples) repo supports building against your local Terminal.Gui source (instead of the NuGet package) to catch breaking changes immediately:

```bash
cd ../Examples
dotnet build -p:TerminalGuiRoot=../Terminal.Gui
```
