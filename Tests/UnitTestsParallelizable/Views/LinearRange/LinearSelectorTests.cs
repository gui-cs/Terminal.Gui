using UnitTests;

namespace ViewsTests;

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
