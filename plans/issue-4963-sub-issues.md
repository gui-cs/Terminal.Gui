# Issue #4963 — FileDialog Keyboard Nav is Broken

## Summary

FileDialog has multiple keyboard navigation and visual bugs introduced during recent updates.
Some are v2.0.0 release blockers. Related closed issue: #4950 (OpenFileDialog required 3 clicks to close — fixed).

---

## Sub-Issues


### 3. Arrow keys in TreeView cause it to resize oddly

**Source:** @tig [comment](https://github.com/gui-cs/Terminal.Gui/issues/4963#issuecomment-4285114363),
@tznind [comment](https://github.com/gui-cs/Terminal.Gui/issues/4963#issuecomment-4291171616)

When the tree has focus, arrow keys navigate correctly but cause the tree view to
grow/resize in unexpected ways. Mouse expand/collapse combined with the splitter
slider also causes odd resizing.

---

### 4. Once nav cycles through once, Cannot Tab to Cancel button, Tree button, or Tree panel

**Source:** @tznind [comment](https://github.com/gui-cs/Terminal.Gui/issues/4963#issuecomment-4291171616)

This is a Bug in Dialog; it reproduces in any dialog with more than 2 focusable controls. After Tab/Shift-Tab cycles through all the controls once, nav breaks.

This repros with Dialog.EnableForDesign.

Issue: https://github.com/gui-cs/Terminal.Gui/issues/5066

