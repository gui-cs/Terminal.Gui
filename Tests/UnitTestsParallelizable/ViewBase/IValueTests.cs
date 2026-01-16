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
}
