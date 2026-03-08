Fixes #4890

## Summary

Phase 1 is implemented and pushed on `feature/kitty`.

Completed:
- added kitty keyboard protocol query, enable, and disable sequences in `EscSeqUtils`
- added `KittyKeyboardProtocolDetector` with structured detection results
- persisted kitty support/enabled state on the ANSI driver
- wired startup detection in `MainLoopCoordinator` so kitty mode is enabled only after a positive response
- moved kitty enable/disable terminal mutation into `AnsiOutput`, including disable on dispose
- added dedicated kitty CSI `u` parsing ahead of broader CSI parsing in `AnsiKeyboardParser`
- added phase-1 compatibility mapping for printable keys, modifiers, navigation/editing keys, and kitty function keys through `F24`
- added focused tests for detector, parser, ANSI lifecycle, startup wiring, and end-to-end ANSI input processing
- broadened parser and integration coverage for kitty navigation/editing sequences plus `F1`-`F12` private-use key codes
- added `Tracing.Trace` lifecycle instrumentation for kitty probe, response, enable, skip, and disable flow in `DEBUG`

## Testing

Passed:
- `dotnet test --project Tests/UnitTestsParallelizable /p:RestorePackagesPath=C:\Users\Tig\.nuget\packages`
- `dotnet test --project Tests/UnitTests -- --filter-class UnitTests.ApplicationTests.MainLoopCoordinatorTests`
- `dotnet test --project Tests/UnitTestsParallelizable /p:RestorePackagesPath=C:\Users\Tig\.nuget\packages -- --filter-class DriverTests.AnsiHandling.KittyKeyboardProtocolDetectorTests --filter-class DriverTests.AnsiHandling.AnsiKeyboardParserTests --filter-class DriverTests.AnsiHandling.AnsiInputTestableTests --filter-class DriverTests.AnsiDriver.AnsiInputOutputTests`

## Design Direction

- Phase 1 keeps compatibility with the current `Key` model.
- Later phases will preserve richer kitty semantics end to end instead of dropping them during parsing or routing.
- `Tracing.Trace` is used for diagnosis only; tests assert release-safe observable behavior rather than trace output.
