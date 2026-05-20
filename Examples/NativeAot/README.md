# Terminal.Gui Native AOT Example

This project is an **AOT-safe mini AllViewsTester**. Unlike UICatalog's `AllViewsView` (which
uses `Activator.CreateInstance` and `MakeGenericType`), this example statically constructs every
`IDesignable` view and calls `EnableForDesign()` to populate demo data.

## What it exercises

Every view type that implements `IDesignable` is instantiated and displayed in a scrollable
list of titled frames. This exercises the full configuration initialization pipeline — 
`ConfigurationManager.Initialize`, `DeepCloner`, `SourceGenerationContext`, and all
config-property-hosting types — under actual AOT trimming.

A `ViewPropertiesEditor` panel (linked from UICatalog's `EditorsAndHelpers/` — no copy,
no reflection) lets you edit properties of the focused view. This proves the editor
infrastructure is also AOT-safe.

Views tested: `Button`, `CheckBox`, `ColorPicker`, `DatePicker`, `DropDownList`,
`FlagSelector`, `GraphView`, `HexView`, `Label`, `Line`, `Link`, `ListView`,
`NumericUpDown`, `OptionSelector`, `ProgressBar`, `ScrollBar`, `Shortcut`, `SpinnerView`,
`Tabs`, `TextField`, `TextValidateField`, `Editor`, `TreeView`, `CharMap`, `FrameView`,
`MenuBar`, `Menu`, `StatusBar`, `MessageBox` (via dialog), `Dialog` (via `Wizard` base).

## Publishing

Native AOT publishing must target the same platform as the host. Cross-compilation is not
supported.

```bash
# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Windows
dotnet publish -c Release -r win-x64 --self-contained
```

## Debugging

To debug the native AOT binary, attach to the process and select `Native Code`.