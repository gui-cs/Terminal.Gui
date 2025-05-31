| Term | Meaning |
|:-----|:--------|
| **Arrange Mode** | Interactive mode activated via `Ctrl+F5` that displays indicators on arrangeable views and allows keyboard-based arrangement. |
| **Arrangement** | The feature of [Layout](~/docs/layout.md) which controls how the user can use the mouse and keyboard to arrange views and enables either **Tiled** or **Overlapped** layouts. |
| **Modal** | A view run as an "application" via @Terminal.Gui.App.Application.Run where `Modal == true`. Has constrained z-order with modal view at z-order 1. |
| **Movable** | A View that can be moved by the user using keyboard or mouse. Enabled by setting @Terminal.Gui.ViewBase.ViewArrangement.Movable flag. |
| **Overlapped** | Layout where SubViews have overlapping Frames with Z-order determining visual stacking. Enabled by @Terminal.Gui.ViewBase.ViewArrangement.Overlapped flag. |
| **Resizable** | A View that can be resized by the user using keyboard or mouse. Enabled by setting @Terminal.Gui.ViewBase.ViewArrangement.Resizable flag. |
| **Runnable** | A view where `Application.Run(Toplevel)` is called. Each non-modal Runnable view has its own `RunState` and operates as a self-contained application. |
| **Tiled** | Layout where SubViews typically do not overlap, with no z-order. Default layout mode set to @Terminal.Gui.ViewBase.ViewArrangement.Fixed. |