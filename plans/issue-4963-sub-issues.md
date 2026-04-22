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

### 4. Cannot Tab to Cancel button or Tree panel

**Source:** @tznind [comment](https://github.com/gui-cs/Terminal.Gui/issues/4963#issuecomment-4291171616)

`Tab`/`Shift-Tab` cannot reach the Cancel button or the Tree panel. @tig's earlier
comment said Tab/Shift-Tab worked everywhere, but @tznind's later testing (with
PR #5281 changes) shows they are unreachable.

---

### 6. SpinnerView doesn't spin during search

**Source:** @tznind [comment](https://github.com/gui-cs/Terminal.Gui/issues/4963#issuecomment-4291171616)

When performing a file search (e.g., navigating to `C:\` and searching for "e"),
the SpinnerView activity indicator does not animate.

---

### 7. Focus should default to the path TextField

**Source:** @tznind [comment](https://github.com/gui-cs/Terminal.Gui/issues/4963#issuecomment-4242959563)

The path text field supports tab-autocomplete and should receive initial focus
when the dialog opens. If it doesn't, that's a bug.
