// Copilot
// Baseline tests for TableView. These lock in current correct behavior
// so that the upcoming redesign (Issue #5064) doesn't introduce silent regressions.
// All tests in this file MUST PASS against the current (pre-refactor) code.

#nullable enable
using System.Data;
using JetBrains.Annotations;
using UnitTests;

namespace ViewsTests;

[TestSubject (typeof (TableView))]
public class TableViewBaselineTests : TestDriverBase
{
    #region Helpers

    private static DataTableSource BuildTable (int cols, int rows) => BuildTable (cols, rows, out _);

    private static DataTableSource BuildTable (int cols, int rows, out DataTable dt)
    {
        dt = new DataTable ();

        for (var c = 0; c < cols; c++)
        {
            dt.Columns.Add ("Col" + c);
        }

        for (var r = 0; r < rows; r++)
        {
            DataRow newRow = dt.NewRow ();

            for (var c = 0; c < cols; c++)
            {
                newRow [c] = $"R{r}C{c}";
            }

            dt.Rows.Add (newRow);
        }

        return new DataTableSource (dt);
    }

    /// <summary>Creates a TableView with the given dimensions and data, fully initialized.</summary>
    private static TableView CreateTableView (int cols, int rows, int viewportWidth = 25, int viewportHeight = 5)
    {
        TableView tv = new ()
        {
            Table = BuildTable (cols, rows),
            MultiSelect = true,
            Viewport = new Rectangle (0, 0, viewportWidth, viewportHeight)
        };
        tv.BeginInit ();
        tv.EndInit ();

        return tv;
    }

    #endregion

    #region A. Arrow Key Cell Movement

    [Fact]
    public void ArrowRight_MovesCursorRight ()
    {
        TableView tv = CreateTableView (5, 10);

        // Table setter puts us at (0,0)
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);

        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (1, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void ArrowDown_MovesCursorDown ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (1, tv.SelectedRow);
    }

    [Fact]
    public void ArrowLeft_AtColumn0_DoesNotGoNegative ()
    {
        TableView tv = CreateTableView (5, 10);
        Assert.Equal (0, tv.SelectedColumn);

        // Left at col 0 — should not go negative
        // HACK: Without Application/focus context, the command returns false
        // and doesn't transfer focus. The key assertion is column stays at 0.
        tv.NewKeyDownEvent (Key.CursorLeft);
        Assert.Equal (0, tv.SelectedColumn);
    }

    [Fact]
    public void ArrowUp_AtRow0_DoesNotGoNegative ()
    {
        TableView tv = CreateTableView (5, 10);
        Assert.Equal (0, tv.SelectedRow);

        tv.NewKeyDownEvent (Key.CursorUp);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void ArrowRight_AtLastColumn_ClampsToLastColumn ()
    {
        TableView tv = CreateTableView (3, 5);
        tv.SelectedColumn = 2; // last column (0-indexed)
        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (2, tv.SelectedColumn);
    }

    [Fact]
    public void ArrowDown_AtLastRow_ClampsToLastRow ()
    {
        TableView tv = CreateTableView (3, 5);
        tv.SelectedRow = 4; // last row (0-indexed)
        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (4, tv.SelectedRow);
    }

    [Fact]
    public void ArrowKeys_MultipleSteps_TraversesGrid ()
    {
        TableView tv = CreateTableView (5, 10);

        // Move to (2, 3)
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorDown);

        Assert.Equal (2, tv.SelectedColumn);
        Assert.Equal (3, tv.SelectedRow);
    }

    #endregion

    #region B. Page/Home/End Navigation

    [Fact]
    public void PageDown_MovesByViewportHeight ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 10);
        Assert.Equal (0, tv.SelectedRow);

        tv.PageDown (false, null);
        Assert.Equal (10, tv.SelectedRow);
    }

    [Fact]
    public void PageUp_MovesByViewportHeight ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 10);
        tv.SelectedRow = 20;

        tv.PageUp (false, null);
        Assert.Equal (10, tv.SelectedRow);
    }

    [Fact]
    public void PageDown_ClampsAtLastRow ()
    {
        TableView tv = CreateTableView (3, 5, viewportHeight: 10);
        Assert.Equal (0, tv.SelectedRow);

        tv.PageDown (false, null);
        Assert.Equal (4, tv.SelectedRow); // last row is 4 (0-indexed, 5 rows)
    }

    [Fact]
    public void PageUp_ClampsAtRow0 ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 10);
        tv.SelectedRow = 3;

        tv.PageUp (false, null);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void Home_Key_MovesToStartOfRow ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 3;

        tv.NewKeyDownEvent (Key.Home);
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow); // row unchanged
    }

    [Fact]
    public void End_Key_MovesToEndOfRow ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 1;

        tv.NewKeyDownEvent (Key.End);
        Assert.Equal (4, tv.SelectedColumn); // last column (0-indexed, 5 cols)
        Assert.Equal (0, tv.SelectedRow); // row unchanged
    }

    [Fact]
    public void ChangeSelectionToStartOfTable_MovesToOrigin ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 3;
        tv.SelectedRow = 7;

        tv.ChangeSelectionToStartOfTable (false, null);
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void ChangeSelectionToEndOfTable_MovesToLastCell ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.ChangeSelectionToEndOfTable (false, null);
        Assert.Equal (4, tv.SelectedColumn);
        Assert.Equal (9, tv.SelectedRow);
    }

    [Fact]
    public void ChangeSelectionToEndOfTable_FullRowSelect_KeepsColumn ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.FullRowSelect = true;
        tv.SelectedColumn = 2;

        tv.ChangeSelectionToEndOfTable (false, null);
        Assert.Equal (2, tv.SelectedColumn); // column preserved with FullRowSelect
        Assert.Equal (9, tv.SelectedRow);
    }

    [Fact]
    public void ChangeSelectionToStartOfRow_API ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 3;
        tv.SelectedRow = 5;

        tv.ChangeSelectionToStartOfRow (false, null);
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (5, tv.SelectedRow); // row unchanged
    }

    [Fact]
    public void ChangeSelectionToEndOfRow_API ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 1;
        tv.SelectedRow = 5;

        tv.ChangeSelectionToEndOfRow (false, null);
        Assert.Equal (4, tv.SelectedColumn);
        Assert.Equal (5, tv.SelectedRow);
    }

    #endregion

    #region C. Selection Changed Events

    [Fact]
    public void ArrowDown_FiresSelectedCellChanged ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        int oldRow = -1;
        int newRow = -1;

        tv.SelectedCellChanged += (_, e) =>
                                  {
                                      fired = true;
                                      oldRow = e.OldRow;
                                      newRow = e.NewRow;
                                  };

        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.True (fired);
        Assert.Equal (0, oldRow);
        Assert.Equal (1, newRow);
    }

    [Fact]
    public void SetSelection_SameValue_DoesNotFireEvent ()
    {
        TableView tv = CreateTableView (5, 10);
        var fireCount = 0;
        tv.SelectedCellChanged += (_, _) => fireCount++;

        // Setting to same value should not fire
        tv.SetSelection (0, 0, false);
        Assert.Equal (0, fireCount);
    }

    [Fact]
    public void SelectedColumn_Set_FiresSelectedCellChanged ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.SelectedCellChanged += (_, _) => fired = true;

        tv.SelectedColumn = 2;
        Assert.True (fired);
    }

    [Fact]
    public void SelectedRow_Set_FiresSelectedCellChanged ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.SelectedCellChanged += (_, _) => fired = true;

        tv.SelectedRow = 3;
        Assert.True (fired);
    }

    #endregion

    #region D. Multi-Select Baseline

    [Fact]
    public void Toggle_AddsCurrentCellToMultiSelect ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 1;
        tv.SelectedRow = 2;

        tv.InvokeCommand (Command.Toggle);
        Assert.True (tv.IsSelected (1, 2));
        Assert.Single (tv.MultiSelectedRegions);
    }

    [Fact]
    public void Toggle_TwiceOnSameCell_RemovesFromMultiSelect ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 1;
        tv.SelectedRow = 2;

        tv.InvokeCommand (Command.Toggle);
        Assert.True (tv.MultiSelectedRegions.Any (r => r.IsToggled));

        tv.InvokeCommand (Command.Toggle);

        // After toggling off, the toggled region should be removed
        Assert.DoesNotContain (tv.MultiSelectedRegions, r => r.IsToggled && r.Rectangle.Contains (1, 2));
    }

    [Fact]
    public void Toggle_MultiSelectFalse_SelectionUnchanged ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.MultiSelect = false;
        tv.SelectedColumn = 1;
        tv.SelectedRow = 2;

        tv.InvokeCommand (Command.Toggle);

        // With MultiSelect=false, toggle should not add regions
        Assert.Empty (tv.MultiSelectedRegions);
    }

    [Fact]
    public void CellToggled_Cancel_PreventsToggle ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 1;
        tv.SelectedRow = 2;

        tv.CellToggled += (_, e) => e.Cancel = true;

        tv.InvokeCommand (Command.Toggle);

        // Cancelled — no toggle should have occurred
        Assert.Empty (tv.MultiSelectedRegions);
    }

    [Fact]
    public void Space_Key_TogglesSelection ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 0;
        tv.SelectedRow = 0;

        tv.NewKeyDownEvent (Key.Space);
        Assert.True (tv.MultiSelectedRegions.Count > 0);
    }

    [Fact]
    public void SelectAll_SelectsEntireTable ()
    {
        TableView tv = CreateTableView (4, 4);
        tv.SelectAll ();
        Assert.Equal (16, tv.GetAllSelectedCells ().Count ());
    }

    [Fact]
    public void SelectAll_MultiSelectFalse_NoEffect ()
    {
        TableView tv = CreateTableView (4, 4);
        tv.MultiSelect = false;
        tv.SelectAll ();

        // Without multi-select, SelectAll is a no-op
        Assert.Empty (tv.MultiSelectedRegions);
    }

    [Fact]
    public void GetAllSelectedCells_NoCursorRegion_ReturnsCursorOnly ()
    {
        TableView tv = CreateTableView (5, 10);
        IEnumerable<Point> cells = tv.GetAllSelectedCells ();
        Assert.Single (cells);
        Assert.Contains (new Point (0, 0), cells);
    }

    [Fact]
    public void IsSelected_CursorCell_ReturnsTrue ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 2;
        tv.SelectedRow = 3;
        Assert.True (tv.IsSelected (2, 3));
    }

    [Fact]
    public void IsSelected_NonCursorCell_ReturnsFalse ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 0;
        tv.SelectedRow = 0;
        Assert.False (tv.IsSelected (1, 1));
    }

    [Fact]
    public void FullRowSelect_IsSelected_ReturnsTrueForEntireRow ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.FullRowSelect = true;
        tv.SelectedRow = 3;

        for (var col = 0; col < 5; col++)
        {
            Assert.True (tv.IsSelected (col, 3), $"Column {col} in selected row should be selected");
        }

        Assert.False (tv.IsSelected (0, 4), "Cell in non-selected row should not be selected");
    }

    [Fact]
    public void ExtendSelection_ShiftRight_CreatesRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 1;
        tv.SelectedRow = 1;

        tv.ChangeSelectionByOffset (1, 0, true, null);

        Assert.True (tv.IsSelected (1, 1), "Origin cell should be selected");
        Assert.True (tv.IsSelected (2, 1), "Extended cell should be selected");
        Assert.Equal (2, tv.SelectedColumn);
    }

    [Fact]
    public void ExtendSelection_ShiftDown_CreatesRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 0;
        tv.SelectedRow = 0;

        tv.ChangeSelectionByOffset (0, 2, true, null);

        Assert.True (tv.IsSelected (0, 0));
        Assert.True (tv.IsSelected (0, 1));
        Assert.True (tv.IsSelected (0, 2));
        Assert.Equal (2, tv.SelectedRow);
    }

    #endregion

    #region E. Edge Cases

    [Fact]
    public void NullTable_ArrowKeysDoNotThrow ()
    {
        TableView tv = new ()
        {
            Viewport = new Rectangle (0, 0, 25, 5)
        };
        tv.BeginInit ();
        tv.EndInit ();

        // Arrow keys are safe with null Table
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorLeft);
        tv.NewKeyDownEvent (Key.CursorUp);
    }

    [Fact]
    public void NullTable_HomeEnd_ThrowsNullReference ()
    {
        // BUG: ChangeSelectionToEndOfRow/StartOfRow use Table! without null check.
        // This documents the current broken behavior. The redesign should fix this.
        TableView tv = new ()
        {
            Viewport = new Rectangle (0, 0, 25, 5)
        };
        tv.BeginInit ();
        tv.EndInit ();

        Assert.Throws<NullReferenceException> (() => tv.NewKeyDownEvent (Key.End));
    }

    [Fact]
    public void NullTable_SelectedColumnAndRow_AreDefaults ()
    {
        TableView tv = new ();
        Assert.Equal (-1, tv.SelectedColumn);
        Assert.Equal (-1, tv.SelectedRow);
    }

    [Fact]
    public void EmptyTable_NoRows_NavigationDoesNotThrow ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col0");
        // 0 rows

        TableView tv = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 25, 5)
        };
        tv.BeginInit ();
        tv.EndInit ();

        tv.NewKeyDownEvent (Key.CursorDown);
        tv.NewKeyDownEvent (Key.CursorRight);
    }

    [Fact]
    public void SingleCell_Table_BoundaryNavigation ()
    {
        TableView tv = CreateTableView (1, 1);
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);

        // Can't move anywhere
        tv.NewKeyDownEvent (Key.CursorRight);
        Assert.Equal (0, tv.SelectedColumn);

        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void SelectedColumn_SetBeyondBounds_Clamped ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 100;
        Assert.Equal (4, tv.SelectedColumn); // clamped to last column
    }

    [Fact]
    public void SelectedRow_SetBeyondBounds_Clamped ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedRow = 100;
        Assert.Equal (9, tv.SelectedRow); // clamped to last row
    }

    [Fact]
    public void SelectedColumn_SetNegative_ClampedToZero ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = -5;
        Assert.Equal (0, tv.SelectedColumn);
    }

    [Fact]
    public void SelectedRow_SetNegative_ClampedToZero ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedRow = -5;
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void SetTable_SetsSelectionToOrigin ()
    {
        TableView tv = new ();
        Assert.Equal (-1, tv.SelectedColumn);
        Assert.Equal (-1, tv.SelectedRow);

        tv.Table = BuildTable (5, 10);

        // Table setter calls SetSelection(0, 0, false)
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void SetTable_Null_AfterHavingData ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 3;
        tv.SelectedRow = 7;

        tv.Table = null;

        // HACK: With null Table, SelectedColumn/SelectedRow retain last value
        // because the setter's clamp logic uses TableIsNullOrInvisible() → clamps to 0.
        // The Table setter calls SetSelection(0,0,...) which goes through SelectedColumn/SelectedRow
        // setters, and those clamp to 0 when Table is null.
        // This is the current behavior being locked in — the redesign will change to null.
        Assert.Equal (0, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void GetAllSelectedCells_EmptyTable_ReturnsEmpty ()
    {
        DataTable dt = new ();
        dt.Columns.Add ("Col0");
        // 0 rows

        TableView tv = new ()
        {
            Table = new DataTableSource (dt),
            Viewport = new Rectangle (0, 0, 25, 5)
        };
        tv.BeginInit ();
        tv.EndInit ();

        IEnumerable<Point> cells = tv.GetAllSelectedCells ();
        Assert.Empty (cells);
    }

    #endregion

    #region F. IValue<Point?> Baseline

    [Fact]
    public void Value_ReflectsCursorPosition ()
    {
        TableView tv = CreateTableView (5, 10);
        Assert.Equal (new Point (0, 0), tv.Value);

        tv.SelectedColumn = 2;
        Assert.Equal (new Point (2, 0), tv.Value);

        tv.SelectedRow = 3;
        Assert.Equal (new Point (2, 3), tv.Value);
    }

    [Fact]
    public void Value_UpdatedByNavigation ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.NewKeyDownEvent (Key.CursorRight);
        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.Equal (new Point (1, 1), tv.Value);
    }

    [Fact]
    public void Value_SetByTableSetter ()
    {
        TableView tv = new ();

        // HACK: Before Table is set, Value is initialized to (-1,-1) in the field initializer.
        // This is the current behavior; the redesign will use null.
        Assert.Equal (new Point (-1, -1), tv.Value);

        tv.Table = BuildTable (5, 10);
        Assert.Equal (new Point (0, 0), tv.Value);
    }

    [Fact]
    public void ValueChanged_FiresOnNavigation ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        Point? oldVal = null;
        Point? newVal = null;

        tv.ValueChanged += (_, e) =>
                           {
                               fired = true;
                               oldVal = e.OldValue;
                               newVal = e.NewValue;
                           };

        tv.NewKeyDownEvent (Key.CursorDown);
        Assert.True (fired);
        Assert.Equal (new Point (0, 0), oldVal);
        Assert.Equal (new Point (0, 1), newVal);
    }

    #endregion

    #region G. Accept / CellActivated

    [Fact]
    public void Accept_Command_FiresCellActivated ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.CellActivated += (_, _) => fired = true;

        tv.InvokeCommand (Command.Accept);
        Assert.True (fired);
    }

    [Fact]
    public void Enter_Key_FiresCellActivated ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.CellActivated += (_, _) => fired = true;

        tv.NewKeyDownEvent (Key.Enter);
        Assert.True (fired);
    }

    [Fact]
    public void Accept_FiresAccepted ()
    {
        TableView tv = CreateTableView (5, 10);
        var fired = false;
        tv.Accepted += (_, _) => fired = true;

        tv.InvokeCommand (Command.Accept);
        Assert.True (fired);
    }

    #endregion

    #region H. EnsureSelectedCellIsVisible

    [Fact]
    public void EnsureSelectedCellIsVisible_NullTable_DoesNotThrow ()
    {
        TableView tv = new ()
        {
            Viewport = new Rectangle (0, 0, 25, 5)
        };

        // Should not throw
        tv.EnsureSelectedCellIsVisible ();
    }

    [Fact]
    public void EnsureSelectedCellIsVisible_ScrollsRowIntoView ()
    {
        TableView tv = CreateTableView (3, 50, viewportHeight: 5);

        // Move to a row that is beyond viewport
        tv.SelectedRow = 20;
        tv.EnsureSelectedCellIsVisible ();

        // After ensuring visibility, Viewport.Y should have adjusted
        // so that row 20 is visible (i.e., Viewport.Y <= 20 < Viewport.Y + Viewport.Height)
        Assert.True (tv.Viewport.Y <= 20, $"Viewport.Y ({tv.Viewport.Y}) should be <= 20");

        // HACK: The exact Viewport.Y depends on header height calculation.
        // We just assert the row is in the visible range.
        int visibleEnd = tv.Viewport.Y + tv.Viewport.Height - 1;
        Assert.True (visibleEnd >= 20, $"Visible end ({visibleEnd}) should be >= 20");
    }

    #endregion

    #region I. ChangeSelectionByOffset

    [Fact]
    public void ChangeSelectionByOffset_Positive_MovesRight ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.ChangeSelectionByOffset (2, 0, false, null);
        Assert.Equal (2, tv.SelectedColumn);
        Assert.Equal (0, tv.SelectedRow);
    }

    [Fact]
    public void ChangeSelectionByOffset_Negative_MovesLeft ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 3;
        tv.ChangeSelectionByOffset (-2, 0, false, null);
        Assert.Equal (1, tv.SelectedColumn);
    }

    [Fact]
    public void ChangeSelectionByOffset_Extend_CreatesMultiSelectRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SelectedColumn = 0;
        tv.SelectedRow = 0;

        tv.ChangeSelectionByOffset (2, 2, true, null);

        Assert.Equal (2, tv.SelectedColumn);
        Assert.Equal (2, tv.SelectedRow);
        Assert.True (tv.IsSelected (0, 0), "Origin should still be selected");
        Assert.True (tv.IsSelected (2, 2), "New position should be selected");
        Assert.True (tv.IsSelected (1, 1), "Cell in between should be selected");
    }

    [Fact]
    public void ChangeSelectionByOffset_ClampsAtBounds ()
    {
        TableView tv = CreateTableView (3, 5);
        tv.SelectedColumn = 2;
        tv.SelectedRow = 4;

        tv.ChangeSelectionByOffset (5, 5, false, null);
        Assert.Equal (2, tv.SelectedColumn); // clamped
        Assert.Equal (4, tv.SelectedRow);    // clamped
    }

    #endregion

    #region J. SetSelection

    [Fact]
    public void SetSelection_MovesToSpecifiedCell ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (3, 7, false);

        Assert.Equal (3, tv.SelectedColumn);
        Assert.Equal (7, tv.SelectedRow);
    }

    [Fact]
    public void SetSelection_Extend_KeepsRegion ()
    {
        TableView tv = CreateTableView (5, 10);
        tv.SetSelection (1, 1, false);
        tv.SetSelection (3, 3, true);

        Assert.Equal (3, tv.SelectedColumn);
        Assert.Equal (3, tv.SelectedRow);
        Assert.True (tv.IsSelected (1, 1), "Origin of extend should be selected");
        Assert.True (tv.IsSelected (2, 2), "Interior cell should be selected");
        Assert.True (tv.IsSelected (3, 3), "End of extend should be selected");
    }

    [Fact]
    public void SetSelection_NoExtend_ClearsOldRegions ()
    {
        TableView tv = CreateTableView (5, 10);

        // Create a multi-select region
        tv.SetSelection (0, 0, false);
        tv.SetSelection (2, 2, true);
        Assert.True (tv.MultiSelectedRegions.Count > 0);

        // Non-extend set clears regions (except toggled ones)
        tv.SetSelection (4, 4, false);
        Assert.False (tv.IsSelected (0, 0), "Old origin should no longer be selected");
        Assert.False (tv.IsSelected (2, 2), "Old extent should no longer be selected");
        Assert.True (tv.IsSelected (4, 4), "New position should be selected");
    }

    #endregion
}
