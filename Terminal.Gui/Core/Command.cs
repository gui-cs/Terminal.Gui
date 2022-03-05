﻿// These classes use a keybinding system based on the design implemented in Scintilla.Net which is an MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

using System;

namespace Terminal.Gui {

	/// <summary>
	/// Actions which can be performed by the application or bound to keys in a <see cref="View"/> control.
	/// </summary>
	public enum Command {

		/// <summary>
		/// Moves the caret down one line.
		/// </summary>
		LineDown,

		/// <summary>
		/// Extends the selection down one line.
		/// </summary>
		LineDownExtend,

		/// <summary>
		/// Moves the caret down to the last child node of the branch that holds the current selection
		/// </summary>
		LineDownToLastBranch,

		/// <summary>
		/// Scrolls down one line (without changing the selection).
		/// </summary>
		ScrollDown,

		// --------------------------------------------------------------------

		/// <summary>
		/// Moves the caret up one line.
		/// </summary>
		LineUp,

		/// <summary>
		/// Extends the selection up one line.
		/// </summary>
		LineUpExtend,

		/// <summary>
		/// Moves the caret up to the first child node of the branch that holds the current selection
		/// </summary>
		LineUpToFirstBranch,

		/// <summary>
		/// Scrolls up one line (without changing the selection).
		/// </summary>
		ScrollUp,

		/// <summary>
		/// Moves the selection left one by the minimum increment supported by the view e.g. single character, cell, item etc.
		/// </summary>
		Left,

		/// <summary>
		/// Scrolls one character to the left
		/// </summary>
		ScrollLeft,

		/// <summary>
		/// Extends the selection left one by the minimum increment supported by the view e.g. single character, cell, item etc.
		/// </summary>
		LeftExtend,

		/// <summary>
		/// Moves the selection right one by the minimum increment supported by the view e.g. single character, cell, item etc.
		/// </summary>
		Right,

		/// <summary>
		/// Scrolls one character to the right.
		/// </summary>
		ScrollRight,

		/// <summary>
		/// Extends the selection right one by the minimum increment supported by the view e.g. single character, cell, item etc.
		/// </summary>
		RightExtend,

		/// <summary>
		/// Moves the caret to the start of the previous word.
		/// </summary>
		WordLeft,

		/// <summary>
		/// Extends the selection to the start of the previous word.
		/// </summary>
		WordLeftExtend,

		/// <summary>
		/// Moves the caret to the start of the next word.
		/// </summary>
		WordRight,

		/// <summary>
		/// Extends the selection to the start of the next word.
		/// </summary>
		WordRightExtend,

		/// <summary>
		/// Deletes and copies to the clipboard the characters from the current position to the end of the line.
		/// </summary>
		CutToEndLine,

		/// <summary>
		/// Deletes and copies to the clipboard the characters from the current position to the start of the line.
		/// </summary>
		CutToStartLine,

		/// <summary>
		/// Deletes the characters forwards.
		/// </summary>
		KillWordForwards,

		/// <summary>
		/// Deletes the characters backwards.
		/// </summary>
		KillWordBackwards,

		/// <summary>
		/// Toggles overwrite mode such that newly typed text overwrites the text that is
		/// already there (typically associated with the Insert key).
		/// </summary>
		ToggleOverwrite,


		/// <summary>
		/// Enables overwrite mode such that newly typed text overwrites the text that is
		/// already there (typically associated with the Insert key).
		/// </summary>
		EnableOverwrite,

		/// <summary>
		/// Disables overwrite mode (<see cref="EnableOverwrite"/>)
		/// </summary>
		DisableOverwrite,

		/// <summary>
		/// Move the page down.
		/// </summary>
		PageDown,

		/// <summary>
		/// Move the page down increase selection area to cover revealed objects/characters.
		/// </summary>
		PageDownExtend,

		/// <summary>
		/// Move the page up.
		/// </summary>
		PageUp,

		/// <summary>
		/// Move the page up increase selection area to cover revealed objects/characters.
		/// </summary>
		PageUpExtend,

		/// <summary>
		/// Moves to top begin.
		/// </summary>
		TopHome,

		/// <summary>
		/// Extends the selection to the top begin.
		/// </summary>
		TopHomeExtend,

		/// <summary>
		/// Moves to bottom end.
		/// </summary>
		BottomEnd,

		/// <summary>
		/// Extends the selection to the bottom end.
		/// </summary>
		BottomEndExtend,

		/// <summary>
		/// Open selected item.
		/// </summary>
		OpenSelectedItem,

		/// <summary>
		/// Toggle the checked state.
		/// </summary>
		ToggleChecked,

		/// <summary>
		/// Accepts the current state (e.g. selection, button press etc)
		/// </summary>
		Accept,

		/// <summary>
		/// Toggles the Expanded or collapsed state of a a list or item (with subitems)
		/// </summary>
		ToggleExpandCollapse,

		/// <summary>
		/// Expands a list or item (with subitems)
		/// </summary>
		Expand,

		/// <summary>
		/// Recursively Expands all child items and their child items (if any)
		/// </summary>
		ExpandAll,

		/// <summary>
		/// Collapses a list or item (with subitems)
		/// </summary>
		Collapse,

		/// <summary>
		/// Recursively collapses a list items of their children (if any)
		/// </summary>
		CollapseAll,

		/// <summary>
		/// Cancels any current temporary states on the control e.g. expanding
		/// a combo list
		/// </summary>
		Cancel,

		/// <summary>
		/// Unix emulation
		/// </summary>
		UnixEmulation,

		/// <summary>
		/// Deletes the character on the right.
		/// </summary>
		DeleteCharRight,

		/// <summary>
		/// Deletes the character on the left.
		/// </summary>
		DeleteCharLeft,

		/// <summary>
		/// Selects all objects in the control.
		/// </summary>
		SelectAll,

		/// <summary>
		/// Deletes all objects in the control.
		/// </summary>
		DeleteAll,

		/// <summary>
		/// Moves the cursor to the start of line.
		/// </summary>
		StartOfLine,

		/// <summary>
		/// Extends the selection to the start of line.
		/// </summary>
		StartOfLineExtend,

		/// <summary>
		/// Moves the cursor to the end of line.
		/// </summary>
		EndOfLine,

		/// <summary>
		/// Extends the selection to the end of line.
		/// </summary>
		EndOfLineExtend,

		/// <summary>
		/// Moves the cursor to the top of page.
		/// </summary>
		StartOfPage,

		/// <summary>
		/// Moves the cursor to the bottom of page.
		/// </summary>
		EndOfPage,

		/// <summary>
		/// Moves to the left page.
		/// </summary>
		PageLeft,

		/// <summary>
		/// Moves to the right page.
		/// </summary>
		PageRight,

		/// <summary>
		/// Moves to the left begin.
		/// </summary>
		LeftHome,

		/// <summary>
		/// Extends the selection to the left begin.
		/// </summary>
		LeftHomeExtend,

		/// <summary>
		/// Moves to the right end.
		/// </summary>
		RightEnd,

		/// <summary>
		/// Extends the selection to the right end.
		/// </summary>
		RightEndExtend,

		/// <summary>
		/// Undo changes.
		/// </summary>
		Undo,

		/// <summary>
		/// Redo changes.
		/// </summary>
		Redo,

		/// <summary>
		/// Copies the current selection.
		/// </summary>
		Copy,

		/// <summary>
		/// Cuts the current selection.
		/// </summary>
		Cut,

		/// <summary>
		/// Pastes the current selection.
		/// </summary>
		Paste,

		/// <summary>
		/// Quit a toplevel.
		/// </summary>
		QuitToplevel,

		/// <summary>
		/// Suspend a application (used on Linux).
		/// </summary>
		Suspend,

		/// <summary>
		/// Moves focus to the next view.
		/// </summary>
		NextView,

		/// <summary>
		/// Moves focuss to the previous view.
		/// </summary>
		PreviousView,

		/// <summary>
		/// Moves focus to the next view or toplevel (case of Mdi).
		/// </summary>
		NextViewOrTop,

		/// <summary>
		/// Moves focus to the next previous or toplevel (case of Mdi).
		/// </summary>
		PreviousViewOrTop,

		/// <summary>
		/// Refresh the application.
		/// </summary>
		Refresh,

		/// <summary>
		/// Toggles the extended selection.
		/// </summary>
		ToggleExtend,

		/// <summary>
		/// Inserts a new line.
		/// </summary>
		NewLine,

		/// <summary>
		/// Inserts a tab.
		/// </summary>
		Tab,

		/// <summary>
		/// Inserts a shift tab.
		/// </summary>
		BackTab
	}
}