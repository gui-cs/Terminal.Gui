---
uid: Terminal.Gui.Views
summary: Built-in UI controls and widgets.
---

The `Views` namespace contains the complete collection of built-in UI controls derived from @Terminal.Gui.ViewBase.View.

## Control Categories

**Basic Controls**
- Label, Button, CheckBox, RadioGroup, ProgressBar

**Text Input**
- TextField, TextView, AutocompleteTextField

**Data Display**
- ListView, TableView, TreeView

**Containers**
- Window, Dialog, FrameView, TabView, TileView

**Selection**
- OptionSeletor, FlagSelector, ColorPicker, DatePicker, Slider, NumericUpDown

**Menus & Navigation**
- MenuBar, ContextMenu, StatusBar, Shortcut

**Dialogs**
- Prompt, MessageBox, FileDialog, OpenDialog, SaveDialog, Wizard

**Specialized**
- CharMap, HexView, GraphView, LineView, ScrollBarView

All views inherit:
- Adornments (Margin, Border, Padding)
- Built-in scrolling
- Focus management
- Keyboard/mouse bindings
- User arrangement (Movable, Resizable)

## See Also

- [Views Overview](~/docs/views.md) - Complete list with examples
- [View Deep Dive](~/docs/View.md) - Base view architecture

