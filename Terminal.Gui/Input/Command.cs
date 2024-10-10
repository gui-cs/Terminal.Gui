// These classes use a keybinding system based on the design implemented in Scintilla.Net which is an
// MIT licensed open source project https://github.com/jacobslusser/ScintillaNET/blob/master/src/ScintillaNET/Command.cs

namespace Terminal.Gui;

/// <summary>
///     Actions which can be performed by a <see cref="View"/>. Commands are typically invoked via
///     <see cref="View.KeyBindings"/> and mouse events.
/// </summary>
public enum Command
{
    #region Base View Commands

    /// <summary>
    ///     Accepts the current state of the View (e.g. list selection, button press, checkbox state, etc.).
    ///     <para>
    ///         The default implementation in <see cref="View"/> calls <see cref="View.RaiseAccepting"/>. If the event is not handled,
    ///         the command is invoked on:
    ///             - Any peer-view that is a <see cref="Button"/> with <see cref="Button.IsDefault"/> set to <see langword="true"/>.
    ///             - The <see cref="View.SuperView"/>. This enables default Accept behavior.
    ///     </para>
    /// </summary>
    Accept,

    /// <summary>
    ///     Performs a hot key action (e.g. setting focus, accepting, and/or moving focus to the next View).
    ///     <para>
    ///         The default implementation in <see cref="View"/> calls <see cref="View.SetFocus"/> and then
    ///         <see cref="View.RaiseHandlingHotKey"/>.
    ///     </para>
    /// </summary>
    HotKey,

    /// <summary>
    ///     Selects the View or an item in the View (e.g. a list item or menu item) without necessarily accepting it.
    ///     <para>
    ///         The default implementation in <see cref="View"/> calls <see cref="View.RaiseSelecting"/>.
    ///     </para>
    /// </summary>
    Select,

    #endregion

    #region Movement Commands

    /// <summary>Moves up one (cell, line, etc...).</summary>
    Up,

    /// <summary>Moves down one item (cell, line, etc...).</summary>
    Down,

    /// <summary>
    ///     Moves left one (cell, line, etc...).
    /// </summary>
    Left,

    /// <summary>
    ///     Moves right one (cell, line, etc...).
    /// </summary>
    Right,

    /// <summary>Move one page up.</summary>
    PageUp,

    /// <summary>Move one page down.</summary>
    PageDown,

    /// <summary>Moves to the left page.</summary>
    PageLeft,

    /// <summary>Moves to the right page.</summary>
    PageRight,

    /// <summary>Moves to the top of page.</summary>
    StartOfPage,

    /// <summary>Moves to the bottom of page.</summary>
    EndOfPage,

    /// <summary>Moves to the start (e.g. the top or home).</summary>
    Start,

    /// <summary>Moves to the end (e.g. the bottom).</summary>
    End,

    /// <summary>Moves left to the start on the current row/line.</summary>
    LeftStart,

    /// <summary>Moves right to the end on the current row/line.</summary>
    RightEnd,

    /// <summary>Moves to the start of the previous word.</summary>
    WordLeft,

    /// <summary>Moves the start of the next word.</summary>
    WordRight,

    #endregion

    #region Movement With Extension Commands

    /// <summary>Extends the selection up one item (cell, line, etc...).</summary>
    UpExtend,

    /// <summary>Extends the selection down one (cell, line, etc...).</summary>
    DownExtend,

    /// <summary>
    ///     Extends the selection left one item (cell, line, etc...)
    /// </summary>
    LeftExtend,

    /// <summary>
    ///     Extends the selection right one item (cell, line, etc...)
    /// </summary>
    RightExtend,

    /// <summary>Extends the selection to the start of the previous word.</summary>
    WordLeftExtend,

    /// <summary>Extends the selection to the start of the next word.</summary>
    WordRightExtend,

    /// <summary>Move one page down extending the selection to cover revealed objects/characters.</summary>
    PageDownExtend,

    /// <summary>Move one page up extending the selection to cover revealed objects/characters.</summary>
    PageUpExtend,

    /// <summary>Extends the selection to start (e.g. home or top).</summary>
    StartExtend,

    /// <summary>Extends the selection to end (e.g. bottom).</summary>
    EndExtend,

    /// <summary>Extends the selection to the start on the current row/line.</summary>
    LeftStartExtend,

    /// <summary>Extends the selection to the right on the current row/line.</summary>
    RightEndExtend,

    /// <summary>Toggles the selection.</summary>
    ToggleExtend,

    #endregion

    #region Editing Commands

    /// <summary>Deletes the characters forwards.</summary>
    KillWordForwards,

    /// <summary>Deletes the characters backwards.</summary>
    KillWordBackwards,

    /// <summary>
    ///     Toggles overwrite mode such that newly typed text overwrites the text that is already there (typically
    ///     associated with the Insert key).
    /// </summary>
    ToggleOverwrite,

    // QUESTION: What is the difference between EnableOverwrite and ToggleOverwrite?

    /// <summary>
    ///     Enables overwrite mode such that newly typed text overwrites the text that is already there (typically
    ///     associated with the Insert key).
    /// </summary>
    EnableOverwrite,

    /// <summary>
    ///     Inserts a character.
    /// </summary>
    Insert,

    /// <summary>Disables overwrite mode (<see cref="EnableOverwrite"/>)</summary>
    DisableOverwrite,

    /// <summary>Deletes the character on the right.</summary>
    DeleteCharRight,

    /// <summary>Deletes the character on the left.</summary>
    DeleteCharLeft,

    /// <summary>Selects all objects.</summary>
    SelectAll,

    /// <summary>Deletes all objects.</summary>
    DeleteAll,

    /// <summary>Inserts a new item.</summary>
    NewLine,

    /// <summary>Unix emulation.</summary>
    UnixEmulation,

    #endregion

    #region Tree Commands

    /// <summary>Moves down to the last child node of the branch that holds the current selection.</summary>
    LineDownToLastBranch,

    /// <summary>Moves up to the first child node of the branch that holds the current selection.</summary>
    LineUpToFirstBranch,

    #endregion

    #region Scroll Commands

    /// <summary>Scrolls down one (cell, line, etc...).</summary>
    ScrollDown,

    /// <summary>Scrolls up one item (cell, line, etc...).</summary>
    ScrollUp,

    /// <summary>Scrolls one item (cell, character, etc...) to the left.</summary>
    ScrollLeft,

    /// <summary>Scrolls one item (cell, character, etc...) to the right.</summary>
    ScrollRight,

    #endregion

    #region Clipboard Commands

    /// <summary>Undo changes.</summary>
    Undo,

    /// <summary>Redo changes.</summary>
    Redo,

    /// <summary>Copies the current selection.</summary>
    Copy,

    /// <summary>Cuts the current selection.</summary>
    Cut,

    /// <summary>Pastes the current selection.</summary>
    Paste,

    /// <summary>Cuts to the clipboard the characters from the current position to the end of the line.</summary>
    CutToEndLine,

    /// <summary>Cuts to the clipboard the characters from the current position to the start of the line.</summary>
    CutToStartLine,

    #endregion

    #region Navigation Commands

    /// <summary>Moves focus to the next <see cref="TabBehavior.TabStop"/>.</summary>
    NextTabStop,

    /// <summary>Moves focus to the previous <see cref="TabBehavior.TabStop"/>.</summary>
    PreviousTabStop,

    /// <summary>Moves focus to the next <see cref="TabBehavior.TabGroup"/>.</summary>
    NextTabGroup,

    /// <summary>Moves focus to the next<see cref="TabBehavior.TabGroup"/>.</summary>
    PreviousTabGroup,

    /// <summary>Tabs to the next item.</summary>
    Tab,

    /// <summary>Tabs back to the previous item.</summary>
    BackTab,

    #endregion

    #region Action Commands

    /// <summary>Toggles something (e.g. the expanded or collapsed state of a list).</summary>
    Toggle,

    /// <summary>Expands a list or item (with subitems).</summary>
    Expand,

    /// <summary>Recursively Expands all child items and their child items (if any).</summary>
    ExpandAll,

    /// <summary>Collapses a list or item (with subitems).</summary>
    Collapse,

    /// <summary>Recursively collapses a list items of their children (if any).</summary>
    CollapseAll,

    /// <summary>Cancels an action or any temporary states on the control e.g. expanding a combo list.</summary>
    Cancel,

    /// <summary>Quit.</summary>
    Quit,

    /// <summary>Refresh.</summary>
    Refresh,

    /// <summary>Suspend an application (Only implemented in <see cref="CursesDriver"/>).</summary>
    Suspend,

    /// <summary>Open the selected item or invoke a UI for opening something.</summary>
    Open,

    /// <summary>Saves the current document.</summary>
    Save,

    /// <summary>Saves the current document with a new name.</summary>
    SaveAs,

    /// <summary>Creates a new document.</summary>
    New,

    /// <summary>Shows context about the item (e.g. a context menu).</summary>
    Context,

    /// <summary>
    ///     Invokes a user interface for editing or configuring something.
    /// </summary>
    Edit,

    #endregion
}