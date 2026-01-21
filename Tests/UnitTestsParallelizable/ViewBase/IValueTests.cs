// Claude - Opus 4.5

using Terminal.Gui.App;

namespace ViewBaseTests;

/// <summary>
///     Unit tests for the <see cref="IValue{TValue}"/> interface using test View subclasses.
/// </summary>
public class IValueTests
{
    /// <summary>
    ///     Test view implementing IValue&lt;int?&gt; using CWPPropertyHelper.
    /// </summary>
    private class TestIntValueView : View, IValue<int?>
    {
        private int? _value;

        public int? Value
        {
            get => _value;
            set => CWPPropertyHelper.ChangeProperty (
                this,
                ref _value,
                value,
                OnValueChanging,
                ValueChanging,
                _ => { }, // No additional work needed for this test view
                OnValueChanged,
                ValueChanged,
                out _);
        }

        public event EventHandler<ValueChangingEventArgs<int?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<int?>>? ValueChanged;

        protected virtual bool OnValueChanging (ValueChangingEventArgs<int?> args) => false;
        protected virtual void OnValueChanged (ValueChangedEventArgs<int?> args) { }
    }

    /// <summary>
    ///     Test view implementing IValue&lt;string?&gt; using CWPPropertyHelper.
    /// </summary>
    private class TestStringValueView : View, IValue<string?>
    {
        private string? _value;

        public string? Value
        {
            get => _value;
            set => CWPPropertyHelper.ChangeProperty (
                this,
                ref _value,
                value,
                OnValueChanging,
                ValueChanging,
                _ => { },
                OnValueChanged,
                ValueChanged,
                out _);
        }

        public event EventHandler<ValueChangingEventArgs<string?>>? ValueChanging;
        public event EventHandler<ValueChangedEventArgs<string?>>? ValueChanged;

        protected virtual bool OnValueChanging (ValueChangingEventArgs<string?> args) => false;
        protected virtual void OnValueChanged (ValueChangedEventArgs<string?> args) { }
    }

    [Fact]
    public void Value_InitiallyNull ()
    {
        TestIntValueView view = new ();
        Assert.Null (view.Value);
    }

    [Fact]
    public void Value_CanBeSet ()
    {
        TestIntValueView view = new ();
        view.Value = 42;
        Assert.Equal (42, view.Value);
    }

    [Fact]
    public void ValueChanged_Fires_WhenValueChanges ()
    {
        TestIntValueView view = new ();
        int? newValue = null;
        int count = 0;

        view.ValueChanged += (_, e) =>
        {
            count++;
            newValue = e.NewValue;
        };

        view.Value = 42;

        Assert.Equal (1, count);
        Assert.Equal (42, newValue);
    }

    [Fact]
    public void ValueChanged_DoesNotFire_WhenValueSame ()
    {
        TestIntValueView view = new ();
        view.Value = 42;
        int count = 0;

        view.ValueChanged += (_, _) => count++;

        view.Value = 42; // Same value

        Assert.Equal (0, count);
    }

    [Fact]
    public void ValueChanging_CanCancel ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        view.ValueChanging += (_, e) =>
        {
            e.Handled = true; // Cancel the change
        };

        view.Value = 42; // Should be cancelled

        Assert.Equal (10, view.Value); // Value unchanged
    }

    [Fact]
    public void ValueChanging_Fires_BeforeValueChanged ()
    {
        TestIntValueView view = new ();
        List<string> events = [];

        view.ValueChanging += (_, _) => events.Add ("changing");
        view.ValueChanged += (_, _) => events.Add ("changed");

        view.Value = 42;

        Assert.Equal (["changing", "changed"], events);
    }

    [Fact]
    public void ValueChanging_ReceivesCurrentAndNewValue ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        int? receivedCurrent = null;
        int? receivedNew = null;

        view.ValueChanging += (_, e) =>
        {
            receivedCurrent = e.CurrentValue;
            receivedNew = e.NewValue;
        };

        view.Value = 42;

        Assert.Equal (10, receivedCurrent);
        Assert.Equal (42, receivedNew);
    }

    [Fact]
    public void ValueChanged_ReceivesOldAndNewValue ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        int? receivedOld = null;
        int? receivedNew = null;

        view.ValueChanged += (_, e) =>
        {
            receivedOld = e.OldValue;
            receivedNew = e.NewValue;
        };

        view.Value = 42;

        Assert.Equal (10, receivedOld);
        Assert.Equal (42, receivedNew);
    }

    [Fact]
    public void StringValue_Works ()
    {
        TestStringValueView view = new ();

        view.Value = "Hello";
        Assert.Equal ("Hello", view.Value);

        view.Value = "World";
        Assert.Equal ("World", view.Value);
    }

    [Fact]
    public void Value_CanBeSetToNull ()
    {
        TestIntValueView view = new ();
        view.Value = 42;

        view.Value = null;

        Assert.Null (view.Value);
    }

    // Tests for non-generic IValue interface

    [Fact]
    public void GetValue_ReturnsBoxedValue_ForInt ()
    {
        TestIntValueView view = new ();
        view.Value = 42;

        IValue nonGeneric = view;
        object? result = nonGeneric.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<int> (result);
        Assert.Equal (42, result);
    }

    [Fact]
    public void GetValue_ReturnsNull_WhenValueIsNull ()
    {
        TestIntValueView view = new ();
        view.Value = null;

        IValue nonGeneric = view;
        object? result = nonGeneric.GetValue ();

        Assert.Null (result);
    }

    [Fact]
    public void GetValue_ReturnsBoxedValue_ForString ()
    {
        TestStringValueView view = new ();
        view.Value = "Hello World";

        IValue nonGeneric = view;
        object? result = nonGeneric.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<string> (result);
        Assert.Equal ("Hello World", result);
    }

    [Fact]
    public void GetValue_ReturnsUpdatedValue_AfterValueChange ()
    {
        TestIntValueView view = new ();
        view.Value = 10;

        IValue nonGeneric = view;
        Assert.Equal (10, nonGeneric.GetValue ());

        view.Value = 99;
        Assert.Equal (99, nonGeneric.GetValue ());
    }

    [Fact]
    public void IValue_CanBeUsedPolymorphically ()
    {
        // Demonstrates that different IValue<T> implementations can be used through IValue
        TestIntValueView intView = new () { Value = 42 };
        TestStringValueView stringView = new () { Value = "test" };

        List<IValue> values = [intView, stringView];

        Assert.Equal (42, values [0].GetValue ());
        Assert.Equal ("test", values [1].GetValue ());
    }

    // Tests for concrete views implementing IValue

    [Fact]
    public void CheckBox_GetValue_ReturnsCheckedState ()
    {
        CheckBox checkBox = new () { CheckedState = CheckState.Checked };

        IValue valueProvider = checkBox;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<CheckState> (result);
        Assert.Equal (CheckState.Checked, result);
    }

    [Fact]
    public void CheckBox_GetValue_ReturnsUnCheckedState ()
    {
        CheckBox checkBox = new () { CheckedState = CheckState.UnChecked };

        IValue valueProvider = checkBox;
        object? result = valueProvider.GetValue ();

        Assert.Equal (CheckState.UnChecked, result);
    }

    [Fact]
    public void TextField_GetValue_ReturnsText ()
    {
        TextField textField = new () { Text = "Hello World" };

        IValue valueProvider = textField;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<string> (result);
        Assert.Equal ("Hello World", result);
    }

    [Fact]
    public void TextField_GetValue_ReturnsEmptyString_WhenEmpty ()
    {
        TextField textField = new ();

        IValue valueProvider = textField;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.Equal ("", result);
    }

    [Fact]
    public void OptionSelector_GetValue_ReturnsSelectedValue ()
    {
        OptionSelector optionSelector = new ()
        {
            Labels = ["Option 1", "Option 2", "Option 3"],
            Value = 1
        };

        IValue valueProvider = optionSelector;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<int> (result);
        Assert.Equal (1, result);
    }

    [Fact]
    public void OptionSelectorT_GetValue_ReturnsTypedValue ()
    {
        OptionSelector<Alignment> optionSelector = new ()
        {
            Value = Alignment.Center
        };

        IValue valueProvider = optionSelector;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<Alignment> (result);
        Assert.Equal (Alignment.Center, result);
    }

    [Fact]
    public void FlagSelectorT_GetValue_ReturnsTypedValue ()
    {
        FlagSelector<AlignmentModes> flagSelector = new ()
        {
            Value = AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems
        };

        IValue valueProvider = flagSelector;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<AlignmentModes> (result);
        Assert.Equal (AlignmentModes.StartToEnd | AlignmentModes.AddSpaceBetweenItems, result);
    }

    [Fact]
    public void NumericUpDown_GetValue_ReturnsValue ()
    {
        NumericUpDown<int> numericUpDown = new () { Value = 42 };

        IValue valueProvider = numericUpDown;
        object? result = valueProvider.GetValue ();

        Assert.NotNull (result);
        Assert.IsType<int> (result);
        Assert.Equal (42, result);
    }

    [Fact]
    public void ColorPicker_GetValue_ReturnsColor ()
    {
        // ColorPicker implements IValue<Color?> with Value property delegating to SelectedColor
        ColorPicker colorPicker = new () { Value = Color.Red };

        IValue valueProvider = colorPicker;
        object? result = valueProvider.GetValue ();

        // The getter returns the SelectedColor which may differ from the set value
        // Just verify we get a Color back
        Assert.NotNull (result);
        Assert.IsType<Color> (result);
    }
}
