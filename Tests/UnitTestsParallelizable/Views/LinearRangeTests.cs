using System.Text;
using UnitTests;

namespace ViewsTests;

public class LinearRangeOptionTests : TestDriverBase
{
    [Fact]
    public void LinearRange_Option_Default_Constructor ()
    {
        LinearRangeOption<int> o = new ();
        Assert.Null (o.Legend);
        Assert.Equal (default (Rune), o.LegendAbbr);
        Assert.Equal (0, o.Data);
    }

    [Fact]
    public void LinearRange_Option_Values_Constructor ()
    {
        LinearRangeOption<int> o = new ("1 thousand", new Rune ('y'), 1000);
        Assert.Equal ("1 thousand", o.Legend);
        Assert.Equal (new Rune ('y'), o.LegendAbbr);
        Assert.Equal (1000, o.Data);
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenEmpty ()
    {
        LinearRangeOption<object> sliderOption = new ();
        Assert.Equal ("{Legend=, LegendAbbr=\0, Data=}", sliderOption.ToString ());
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenPopulated_WithInt ()
    {
        LinearRangeOption<int> sliderOption = new () { Legend = "Lord flibble", LegendAbbr = new Rune ('l'), Data = 1 };

        Assert.Equal ("{Legend=Lord flibble, LegendAbbr=l, Data=1}", sliderOption.ToString ());
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenPopulated_WithSizeF ()
    {
        LinearRangeOption<SizeF> sliderOption = new () { Legend = "Lord flibble", LegendAbbr = new Rune ('l'), Data = new SizeF (32, 11) };

        Assert.Equal ("{Legend=Lord flibble, LegendAbbr=l, Data={Width=32, Height=11}}", sliderOption.ToString ());
    }

    [Fact]
    public void OnChanged_Should_Raise_ChangedEvent ()
    {
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.Changed += (sender, args) => eventRaised = true;

        sliderOption.OnChanged (true);

        Assert.True (eventRaised);
    }

    [Fact]
    public void OnSet_Should_Raise_SetEvent ()
    {
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.Set += (sender, args) => eventRaised = true;

        sliderOption.OnSet ();

        Assert.True (eventRaised);
    }

    [Fact]
    public void OnUnSet_Should_Raise_UnSetEvent ()
    {
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.UnSet += (sender, args) => eventRaised = true;

        sliderOption.OnUnSet ();

        Assert.True (eventRaised);
    }
}

public class LinearRangeEventArgsTests : TestDriverBase
{
    [Fact]
    public void Constructor_Sets_Cancel_Default_To_False ()
    {
        Dictionary<int, LinearRangeOption<int>> options = new ();
        var focused = 42;

        LinearRangeEventArgs<int> sliderEventArgs = new (options, focused);

        Assert.False (sliderEventArgs.Cancel);
    }

    [Fact]
    public void Constructor_Sets_Focused ()
    {
        Dictionary<int, LinearRangeOption<int>> options = new ();
        var focused = 42;

        LinearRangeEventArgs<int> sliderEventArgs = new (options, focused);

        Assert.Equal (focused, sliderEventArgs.Focused);
    }

    [Fact]
    public void Constructor_Sets_Options ()
    {
        Dictionary<int, LinearRangeOption<int>> options = new ();

        LinearRangeEventArgs<int> sliderEventArgs = new (options);

        Assert.Equal (options, sliderEventArgs.Options);
    }
}

// =============================================================================
// LinearSelector<T> tests
// =============================================================================
public class LinearSelectorTests : TestDriverBase
{
    [Fact]
    public void Constructor_Default ()
    {
        LinearSelector<int> sel = new ();

        Assert.NotNull (sel);
        Assert.NotNull (sel.Options);
        Assert.Empty (sel.Options);
        Assert.Equal (Orientation.Horizontal, sel.Orientation);
        Assert.False (sel.AllowEmpty);
        Assert.True (sel.ShowLegends);
        Assert.False (sel.ShowEndSpacing);
        Assert.Equal (1, sel.MinimumInnerSpacing);
        Assert.True (sel.Width is DimAuto);
        Assert.True (sel.Height is DimAuto);
        Assert.Equal (0, sel.FocusedOption);
        Assert.Equal (default, sel.Value);
    }

    [Fact]
    public void Constructor_With_Options ()
    {
        List<int> options = [1, 2, 3];

        LinearSelector<int> sel = new (options);
        sel.SetRelativeLayout (new Size (100, 100));

        // 1 2 3
        Assert.Equal (1, sel.MinimumInnerSpacing);
        Assert.Equal (new Size (5, 2), sel.GetContentSize ());
        Assert.Equal (new Size (5, 2), sel.Frame.Size);
        Assert.Equal (options.Count, sel.Options.Count);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Selects_Matching_Option ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);

        sel.Value = 20;

        Assert.Equal (20, sel.Value);
        Assert.Single (sel.SelectedIndices);
        Assert.Equal (1, sel.SelectedIndices [0]);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Null_Clears_Selection ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);
        sel.Value = 20;

        sel.Value = default;

        Assert.Equal (default, sel.Value);
        Assert.Empty (sel.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Unmatched_Clears_Indices ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);

        sel.Value = 99;

        Assert.Equal (99, sel.Value);
        Assert.Empty (sel.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Same_Value_Does_Not_Raise_Events ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]) { Value = 20 };
        var changedCount = 0;
        sel.ValueChanged += (_, _) => changedCount++;

        sel.Value = 20;

        Assert.Equal (0, changedCount);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Raises_ValueChanging_And_ValueChanged ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);
        var changingRaised = false;
        var changedRaised = false;

        sel.ValueChanging += (_, args) =>
                             {
                                 changingRaised = true;
                                 Assert.Equal (default, args.CurrentValue);
                                 Assert.Equal (20, args.NewValue);
                             };

        sel.ValueChanged += (_, args) =>
                            {
                                changedRaised = true;
                                Assert.Equal (default, args.OldValue);
                                Assert.Equal (20, args.NewValue);
                            };

        sel.Value = 20;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
    }

    // Copilot
    [Fact]
    public void Value_Setter_ValueChanging_Cancellation_Reverts ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);

        sel.ValueChanging += (_, args) => args.Handled = true;

        sel.Value = 20;

        Assert.Equal (default, sel.Value);
        Assert.Empty (sel.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Internal_Selection_Syncs_Value ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);
        sel.FocusedOption = 1;

        sel.InvokeCommand (Command.Activate);

        Assert.Equal (20, sel.Value);
        Assert.Single (sel.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Internal_Selection_Raises_ValueChanged_And_ValueChangedUntyped ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);
        var typedRaised = false;
        var untypedRaised = false;

        sel.ValueChanged += (_, args) =>
                            {
                                typedRaised = true;
                                Assert.Equal (default, args.OldValue);
                                Assert.Equal (20, args.NewValue);
                            };

        IValue ivalue = sel;
        ivalue.ValueChangedUntyped += (_, args) =>
                                      {
                                          untypedRaised = true;
                                          Assert.Equal (20, args.NewValue);
                                      };

        sel.FocusedOption = 1;
        sel.InvokeCommand (Command.Activate);

        Assert.True (typedRaised);
        Assert.True (untypedRaised);
    }

    // Copilot
    [Fact]
    public void IValue_GetValue_Returns_Boxed_Value ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]);
        sel.Value = 30;

        IValue ivalue = sel;

        Assert.Equal (30, ivalue.GetValue ());
    }

    // Copilot
    [Fact]
    public void Options_Replacement_Drops_Stale_Indices ()
    {
        LinearSelector<int> sel = new ([10, 20, 30]) { Value = 30 };

        Assert.Single (sel.SelectedIndices);

        sel.Options = [new LinearRangeOption<int> { Data = 1 }];

        // Index 2 from previous selection no longer exists.
        Assert.Empty (sel.SelectedIndices);
    }
}

// =============================================================================
// LinearMultiSelector<T> tests
// =============================================================================
public class LinearMultiSelectorTests : TestDriverBase
{
    [Fact]
    public void Constructor_Default ()
    {
        LinearMultiSelector<string> ms = new ();

        Assert.NotNull (ms);
        Assert.NotNull (ms.Value);
        Assert.Empty (ms.Value!);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Selects_Matching_Options ()
    {
        LinearMultiSelector<string> ms = new (["A", "B", "C"]);

        ms.Value = ["A", "C"];

        Assert.Equal (2, ms.Value!.Count);
        Assert.Equal (2, ms.SelectedIndices.Count);
        Assert.Contains (0, ms.SelectedIndices);
        Assert.Contains (2, ms.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Null_Treated_As_Empty ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]) { Value = ["A"] };

        ms.Value = null;

        Assert.NotNull (ms.Value);
        Assert.Empty (ms.Value!);
        Assert.Empty (ms.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_SequenceEqual_Does_Not_Raise_Events ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]) { Value = ["A", "B"] };
        var changedCount = 0;
        ms.ValueChanged += (_, _) => changedCount++;

        // Different list instance, same elements/order.
        ms.Value = ["A", "B"];

        Assert.Equal (0, changedCount);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Defensive_Copy_Of_Input ()
    {
        LinearMultiSelector<string> ms = new (["A", "B", "C"]);
        List<string> mutable = ["A"];

        ms.Value = mutable;

        // Mutate caller's list after assignment.
        mutable.Add ("B");

        Assert.Single (ms.Value!);
        Assert.Equal ("A", ms.Value! [0]);
    }

    // Copilot
    [Fact]
    public void Value_Getter_Never_Returns_Null ()
    {
        LinearMultiSelector<string> ms = new (["A"]);

        Assert.NotNull (ms.Value);
    }

    // Copilot
    [Fact]
    public void Value_Setter_ValueChanging_Cancellation_Reverts ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]);
        ms.ValueChanging += (_, args) => args.Handled = true;

        ms.Value = ["A"];

        Assert.Empty (ms.Value!);
        Assert.Empty (ms.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Internal_Multi_Selection_Builds_Sorted_Value ()
    {
        LinearMultiSelector<string> ms = new (["A", "B", "C"]) { AllowEmpty = true };

        // Activate index 2 then index 0.
        ms.FocusedOption = 2;
        ms.InvokeCommand (Command.Activate);

        ms.FocusedOption = 0;
        ms.InvokeCommand (Command.Activate);

        // Value is built in option-order, not selection-order.
        Assert.Equal (2, ms.Value!.Count);
        Assert.Equal ("A", ms.Value! [0]);
        Assert.Equal ("C", ms.Value! [1]);
    }

    // Copilot
    [Fact]
    public void IValue_GetValue_Returns_Boxed_List ()
    {
        LinearMultiSelector<string> ms = new (["A", "B"]) { Value = ["A"] };
        IValue ivalue = ms;

        object? boxed = ivalue.GetValue ();

        IReadOnlyList<string>? list = boxed as IReadOnlyList<string>;
        Assert.NotNull (list);
        Assert.Single (list!);
        Assert.Equal ("A", list! [0]);
    }
}

// =============================================================================
// LinearRange<T> (range-only) tests
// =============================================================================
public class LinearRangeRangeTests : TestDriverBase
{
    [Fact]
    public void Constructor_Default ()
    {
        LinearRange<int> r = new ();

        Assert.NotNull (r);
        Assert.Equal (LinearRangeSpanKind.Closed, r.RangeKind);
        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Closed_Selects_Both_Bounds ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, 1, 3);

        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (20, r.Value.Start);
        Assert.Equal (40, r.Value.End);
        Assert.Contains (1, r.SelectedIndices);
        Assert.Contains (3, r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Resolves_Indices_From_Data_When_NotProvided ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, -1, -1);

        // The setter should resolve indices via IndexOfData.
        Assert.Contains (1, r.SelectedIndices);
        Assert.Contains (3, r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_LeftBounded_Selects_End_Only ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.LeftBounded };

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 20, -1, 1);

        Assert.Single (r.SelectedIndices);
        Assert.Equal (1, r.SelectedIndices [0]);
        Assert.Equal (20, r.Value.End);
    }

    // Copilot
    [Fact]
    public void Value_Setter_RightBounded_Selects_Start_Only ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.RightBounded };

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.RightBounded, 20, default, 1, -1);

        Assert.Single (r.SelectedIndices);
        Assert.Equal (1, r.SelectedIndices [0]);
        Assert.Equal (20, r.Value.Start);
    }

    // Copilot
    [Fact]
    public void Value_Setter_None_Clears ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);

        r.Value = LinearRangeSpan<int>.Empty;

        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
        Assert.Empty (r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void Value_Setter_Same_Value_Does_Not_Raise_Events ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);
        var changedCount = 0;
        r.ValueChanged += (_, _) => changedCount++;

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);

        Assert.Equal (0, changedCount);
    }

    // Copilot
    [Fact]
    public void Value_Setter_ValueChanging_Cancellation_Reverts ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.ValueChanging += (_, args) => args.Handled = true;

        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);

        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
        Assert.Empty (r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void RangeKind_Closed_To_LeftBounded_Drops_Start ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, 1, 3);

        r.RangeKind = LinearRangeSpanKind.LeftBounded;

        Assert.Equal (LinearRangeSpanKind.LeftBounded, r.Value.Kind);
        Assert.Equal (40, r.Value.End);
        Assert.Equal (3, r.Value.EndIndex);
        Assert.Equal (default, r.Value.Start);
        Assert.Equal (-1, r.Value.StartIndex);
    }

    // Copilot
    [Fact]
    public void RangeKind_Closed_To_RightBounded_Drops_End ()
    {
        LinearRange<int> r = new ([10, 20, 30, 40]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 20, 40, 1, 3);

        r.RangeKind = LinearRangeSpanKind.RightBounded;

        Assert.Equal (LinearRangeSpanKind.RightBounded, r.Value.Kind);
        Assert.Equal (20, r.Value.Start);
        Assert.Equal (1, r.Value.StartIndex);
        Assert.Equal (default, r.Value.End);
        Assert.Equal (-1, r.Value.EndIndex);
    }

    // Copilot
    [Fact]
    public void RangeKind_LeftBounded_To_RightBounded_Promotes_End_To_Start ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.LeftBounded };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 20, -1, 1);

        r.RangeKind = LinearRangeSpanKind.RightBounded;

        Assert.Equal (LinearRangeSpanKind.RightBounded, r.Value.Kind);
        Assert.Equal (20, r.Value.Start);
        Assert.Equal (1, r.Value.StartIndex);
    }

    // Copilot
    [Fact]
    public void RangeKind_LeftBounded_To_Closed_Collapses_End_To_Both ()
    {
        LinearRange<int> r = new ([10, 20, 30]) { RangeKind = LinearRangeSpanKind.LeftBounded };
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.LeftBounded, default, 30, -1, 2);

        r.RangeKind = LinearRangeSpanKind.Closed;

        Assert.Equal (LinearRangeSpanKind.Closed, r.Value.Kind);
        Assert.Equal (30, r.Value.Start);
        Assert.Equal (30, r.Value.End);
    }

    // Copilot
    [Fact]
    public void RangeKind_To_None_Clears ()
    {
        LinearRange<int> r = new ([10, 20]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 20, 0, 1);

        r.RangeKind = LinearRangeSpanKind.None;

        Assert.Equal (LinearRangeSpanKind.None, r.Value.Kind);
        Assert.Empty (r.SelectedIndices);
    }

    // Copilot
    [Fact]
    public void LinearRangeSpan_Equality_Is_ByValue ()
    {
        LinearRangeSpan<int> a = new (LinearRangeSpanKind.Closed, 10, 20, 0, 1);
        LinearRangeSpan<int> b = new (LinearRangeSpanKind.Closed, 10, 20, 0, 1);

        Assert.Equal (a, b);
        Assert.True (a == b);
        Assert.Equal (a.GetHashCode (), b.GetHashCode ());
    }

    // Copilot
    [Fact]
    public void LinearRangeSpan_Empty_Has_None_Kind ()
    {
        LinearRangeSpan<int> empty = LinearRangeSpan<int>.Empty;

        Assert.Equal (LinearRangeSpanKind.None, empty.Kind);
        Assert.Equal (-1, empty.StartIndex);
        Assert.Equal (-1, empty.EndIndex);
    }

    // Copilot
    [Fact]
    public void IValue_GetValue_Returns_Boxed_Span ()
    {
        LinearRange<int> r = new ([10, 20, 30]);
        r.Value = new LinearRangeSpan<int> (LinearRangeSpanKind.Closed, 10, 30, 0, 2);
        IValue ivalue = r;

        object? boxed = ivalue.GetValue ();

        Assert.IsType<LinearRangeSpan<int>> (boxed);
        Assert.Equal (r.Value, (LinearRangeSpan<int>)boxed!);
    }
}

// =============================================================================
// Shared (base) behavior tests via LinearSelector<T>
// =============================================================================
public class LinearRangeViewBaseTests : TestDriverBase
{
    [Fact]
    public void MovePlus_Should_MoveFocusRight_When_OptionIsAvailable ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        bool result = sel.MovePlus ();

        Assert.True (result);
        Assert.Equal (1, sel.FocusedOption);
    }

    [Fact]
    public void MovePlus_Should_NotMoveFocusRight_When_AtEnd ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        sel.FocusedOption = 3;

        bool result = sel.MovePlus ();

        Assert.False (result);
        Assert.Equal (3, sel.FocusedOption);
    }

    [Fact]
    public void OnOptionFocused_Event_Cancelled ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        var eventRaised = false;
        sel.OptionFocused += (sender, args) => eventRaised = true;
        var newFocusedOption = 1;

        LinearRangeEventArgs<int> args = new (new Dictionary<int, LinearRangeOption<int>> (), newFocusedOption) { Cancel = false };
        Assert.Equal (0, sel.FocusedOption);

        sel.OnOptionFocused (newFocusedOption, args);

        Assert.True (eventRaised);
        Assert.Equal (newFocusedOption, sel.FocusedOption);

        args = new LinearRangeEventArgs<int> (new Dictionary<int, LinearRangeOption<int>> (), newFocusedOption) { Cancel = true };

        sel.OnOptionFocused (2, args);

        Assert.True (eventRaised);
        Assert.Equal (newFocusedOption, sel.FocusedOption);
    }

    [Fact]
    public void OnOptionFocused_Event_Raised ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        var eventRaised = false;
        sel.OptionFocused += (sender, args) => eventRaised = true;
        var newFocusedOption = 1;
        LinearRangeEventArgs<int> args = new (new Dictionary<int, LinearRangeOption<int>> (), newFocusedOption);

        sel.OnOptionFocused (newFocusedOption, args);

        Assert.True (eventRaised);
    }

    [Fact]
    public void Set_Should_Not_Clear_When_EmptyNotAllowed ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]) { AllowEmpty = false };

        Assert.NotEmpty (sel.SelectedIndices);

        // Re-activating the same focused option must not clear it when AllowEmpty=false.
        sel.InvokeCommand (Command.Activate);

        Assert.NotEmpty (sel.SelectedIndices);
    }

    [Fact]
    public void Set_Should_SetFocusedOption ()
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        sel.FocusedOption = 2;
        bool result = sel.InvokeCommand (Command.Activate) ?? false;

        Assert.Equal (2, sel.FocusedOption);
        Assert.Single (sel.SelectedIndices);
    }

    [Fact]
    public void TryGetOptionByPosition_InvalidPosition_Failure ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        var x = 10;
        var y = 10;
        var threshold = 2;
        int expectedOption = -1;

        bool result = sel.TryGetOptionByPosition (x, y, threshold, out int option);

        Assert.False (result);
        Assert.Equal (expectedOption, option);
    }

    [Theory]
    [InlineData (0, 0, 0, 1)]
    [InlineData (3, 0, 0, 2)]
    [InlineData (9, 0, 0, 4)]
    [InlineData (0, 0, 1, 1)]
    [InlineData (3, 0, 1, 2)]
    [InlineData (9, 0, 1, 4)]
    public void TryGetOptionByPosition_ValidPositionHorizontal_Success (int x, int y, int threshold, int expectedData)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);

        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetOptionByPosition (x, y, threshold, out int option);

        Assert.True (result);
        Assert.Equal (expectedData, sel.Options [option].Data);
    }

    [Theory]
    [InlineData (0, 0, 0, 1)]
    [InlineData (0, 3, 0, 2)]
    [InlineData (0, 9, 0, 4)]
    [InlineData (0, 0, 1, 1)]
    [InlineData (0, 3, 1, 2)]
    [InlineData (0, 9, 1, 4)]
    public void TryGetOptionByPosition_ValidPositionVertical_Success (int x, int y, int threshold, int expectedData)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);
        sel.Orientation = Orientation.Vertical;
        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetOptionByPosition (x, y, threshold, out int option);

        Assert.True (result);
        Assert.Equal (expectedData, sel.Options [option].Data);
    }

    [Fact]
    public void TryGetPositionByOption_InvalidOption_Failure ()
    {
        LinearSelector<int> sel = new ([1, 2, 3]);
        int option = -1;
        (int, int) expectedPosition = (-1, -1);

        bool result = sel.TryGetPositionByOption (option, out (int x, int y) position);

        Assert.False (result);
        Assert.Equal (expectedPosition, position);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 3, 0)]
    [InlineData (3, 9, 0)]
    public void TryGetPositionByOption_ValidOptionHorizontal_Success (int option, int expectedX, int expectedY)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);
        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetPositionByOption (option, out (int x, int y) position);

        Assert.True (result);
        Assert.Equal (expectedX, position.x);
        Assert.Equal (expectedY, position.y);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 0, 3)]
    [InlineData (3, 0, 9)]
    public void TryGetPositionByOption_ValidOptionVertical_Success (int option, int expectedX, int expectedY)
    {
        LinearSelector<int> sel = new ([1, 2, 3, 4]);
        sel.Orientation = Orientation.Vertical;
        sel.MinimumInnerSpacing = 2;

        bool result = sel.TryGetPositionByOption (option, out (int x, int y) position);

        Assert.True (result);
        Assert.Equal (expectedX, position.x);
        Assert.Equal (expectedY, position.y);
    }

    [Fact]
    private void DimAuto_Both_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<object> options = ["01234", "01234"];

        LinearMultiSelector<object> ms = new (options) { Orientation = Orientation.Vertical };
        view.Add (ms);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = ms.Frame.Size;

        Assert.Equal (new Size (6, 3), expectedSize);

        view.SetContentSize (new Size (1, 1));

        view.LayoutSubViews ();
        ms.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, ms.Frame.Size);
    }

    [Fact]
    private void DimAuto_Height_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = 10, Height = Dim.Fill () };

        List<object> options = ["01234", "01234"];

        LinearMultiSelector<object> ms = new (options) { Orientation = Orientation.Vertical, Width = 10 };
        view.Add (ms);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = ms.Frame.Size;

        Assert.Equal (new Size (10, 3), expectedSize);

        view.SetContentSize (new Size (1, 1));

        view.LayoutSubViews ();
        ms.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, ms.Frame.Size);
    }

    [Fact]
    private void DimAuto_Width_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = Dim.Fill (), Height = 10 };

        List<object> options = ["01234", "01234"];

        LinearMultiSelector<object> ms = new (options) { Orientation = Orientation.Vertical, Height = 10 };
        view.Add (ms);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = ms.Frame.Size;

        Assert.Equal (new Size (6, 10), expectedSize);

        view.SetContentSize (new Size (1, 1));

        view.LayoutSubViews ();
        ms.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, ms.Frame.Size);
    }

    // https://github.com/gui-cs/Terminal.Gui/issues/3099
    [Fact]
    private void One_Option_Does_Not_Throw ()
    {
        LinearSelector<int> sel = new ();
        sel.BeginInit ();
        sel.EndInit ();

        sel.Options = [new LinearRangeOption<int> ()];
    }
}

// =============================================================================
// CWP property tests (Type/TypeChanging/etc removed; remaining CWP props live on)
// =============================================================================
public class LinearRangeCWPTests : TestDriverBase
{
    [Fact]
    public void LegendsOrientation_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = Orientation.Horizontal;
        var newValue = Orientation.Vertical;

        sel.LegendsOrientationChanging += (sender, args) =>
                                          {
                                              changingRaised = true;
                                              Assert.Equal (oldValue, args.CurrentValue);
                                              Assert.Equal (newValue, args.NewValue);
                                          };

        sel.LegendsOrientationChanged += (sender, args) =>
                                         {
                                             changedRaised = true;
                                             Assert.Equal (oldValue, args.OldValue);
                                             Assert.Equal (newValue, args.NewValue);
                                         };

        sel.LegendsOrientation = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.LegendsOrientation);
    }

    [Fact]
    public void MinimumInnerSpacing_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = 1;
        var newValue = 5;

        sel.MinimumInnerSpacingChanging += (sender, args) =>
                                           {
                                               changingRaised = true;
                                               Assert.Equal (oldValue, args.CurrentValue);
                                               Assert.Equal (newValue, args.NewValue);
                                           };

        sel.MinimumInnerSpacingChanged += (sender, args) =>
                                          {
                                              changedRaised = true;
                                              Assert.Equal (oldValue, args.OldValue);
                                              Assert.Equal (newValue, args.NewValue);
                                          };

        sel.MinimumInnerSpacing = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.MinimumInnerSpacing);
    }

    [Fact]
    public void ShowEndSpacing_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = false;
        var newValue = true;

        sel.ShowEndSpacingChanging += (sender, args) =>
                                      {
                                          changingRaised = true;
                                          Assert.Equal (oldValue, args.CurrentValue);
                                          Assert.Equal (newValue, args.NewValue);
                                      };

        sel.ShowEndSpacingChanged += (sender, args) =>
                                     {
                                         changedRaised = true;
                                         Assert.Equal (oldValue, args.OldValue);
                                         Assert.Equal (newValue, args.NewValue);
                                     };

        sel.ShowEndSpacing = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.ShowEndSpacing);
    }

    [Fact]
    public void ShowLegends_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = true;
        var newValue = false;

        sel.ShowLegendsChanging += (sender, args) =>
                                   {
                                       changingRaised = true;
                                       Assert.Equal (oldValue, args.CurrentValue);
                                       Assert.Equal (newValue, args.NewValue);
                                   };

        sel.ShowLegendsChanged += (sender, args) =>
                                  {
                                      changedRaised = true;
                                      Assert.Equal (oldValue, args.OldValue);
                                      Assert.Equal (newValue, args.NewValue);
                                  };

        sel.ShowLegends = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.ShowLegends);
    }

    [Fact]
    public void UseMinimumSize_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        LinearSelector<int> sel = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = false;
        var newValue = true;

        sel.UseMinimumSizeChanging += (sender, args) =>
                                      {
                                          changingRaised = true;
                                          Assert.Equal (oldValue, args.CurrentValue);
                                          Assert.Equal (newValue, args.NewValue);
                                      };

        sel.UseMinimumSizeChanged += (sender, args) =>
                                     {
                                         changedRaised = true;
                                         Assert.Equal (oldValue, args.OldValue);
                                         Assert.Equal (newValue, args.NewValue);
                                     };

        sel.UseMinimumSize = newValue;

        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, sel.UseMinimumSize);
    }

    // Copilot
    [Fact]
    public void Command_Activate_Calls_SetFocusedOption ()
    {
        LinearSelector<int> sel = new ();

        sel.Options =
        [
            new LinearRangeOption<int> ("A", new Rune ('a'), 1),
            new LinearRangeOption<int> ("B", new Rune ('b'), 2),
            new LinearRangeOption<int> ("C", new Rune ('c'), 3)
        ];

        sel.FocusedOption = 1;

        bool? result = sel.InvokeCommand (Command.Activate);

        Assert.False (result);
        Assert.Contains (1, sel.SelectedIndices);

        sel.Dispose ();
    }

    // Copilot
    [Fact]
    public void Command_Accept_Calls_SetFocusedOption ()
    {
        LinearSelector<int> sel = new ();

        sel.Options =
        [
            new LinearRangeOption<int> ("A", new Rune ('a'), 1),
            new LinearRangeOption<int> ("B", new Rune ('b'), 2),
            new LinearRangeOption<int> ("C", new Rune ('c'), 3)
        ];

        sel.FocusedOption = 2;

        sel.InvokeCommand (Command.Accept);

        Assert.Contains (2, sel.SelectedIndices);

        sel.Dispose ();
    }
}
