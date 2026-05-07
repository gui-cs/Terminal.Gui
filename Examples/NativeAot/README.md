# Terminal.Gui Native AOT Example

This project tests the `Terminal.Gui` library as a native AOT application, ensuring that
AOT-sensitive code paths (configuration deep-cloning, JSON serialization, dictionary
construction) work correctly after trimming.

## What it exercises

The example deliberately instantiates views from all major config-property-hosting types:
`Button`, `CheckBox`, `Dialog` (via `MessageBox`), `FrameView`, `Label`, `MenuBar`, `Menu`,
`OptionSelector` (`SelectorBase`), `StatusBar`, `TextField`, `TextView`, `Window`.

This ensures that `DeepCloner`, `SourceGenerationContext`, and `ConfigurationManager.Initialize`
exercise the typed and JSON-based dictionary construction paths that are most sensitive to AOT
trimming.

## Publishing

Unlike self-contained single-file publishing, native AOT publishing must be generated on the
same platform as the target. Cross-compilation is not supported.

```bash
# Linux
dotnet publish -c Release -r linux-x64 --self-contained

# macOS
dotnet publish -c Release -r osx-x64 --self-contained

# Windows
dotnet publish -c Release -r win-x64 --self-contained
```

## Debugging

When executing directly from the native AOT binary and needing to debug, attach to the
debugger like any other standalone application and select `Native Code`.