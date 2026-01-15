# AttributePicker Implementation Plan

## Overview

Create a new `AttributePicker` View that combines two `ColorPicker` subviews (foreground and background), a `FlagSelector<TextStyle>` for text styles, and a sample text preview showing how the selected attribute will look.

This implementation also introduces the `IValue<TValue>` interface, which will enable automatic result extraction in `Prompt<TView, TResult>` for views that implement it.

## Files to Create

```
Terminal.Gui/ViewBase/IValue.cs
Terminal.Gui/Views/Color/AttributePicker.cs
Tests/UnitTestsParallelizable/ViewBase/IValueTests.cs
Tests/UnitTestsParallelizable/Views/AttributePickerTests.cs
```

---

## Part 1: IValue<TValue> Interface

### Interface Definition

```csharp
// Terminal.Gui/ViewBase/IValue.cs
using Terminal.Gui.App;

namespace Terminal.Gui;

/// <summary>
///     Interface for views that provide a strongly-typed value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
/// <remarks>
///     <para>
///         Views implementing this interface can be used with <c>Prompt&lt;TView, TResult&gt;</c>
///         for automatic result extraction without requiring an explicit <c>resultExtractor</c>.
///     </para>
///     <para>
///         Implementers should use <see cref="CWPPropertyHelper.ChangeProperty{T}"/> to implement
///         the <see cref="Value"/> property setter, which follows the Cancellable Work Pattern (CWP).
///     </para>
/// </remarks>
/// <seealso cref="CWPPropertyHelper"/>
/// <seealso cref="ValueChangingEventArgs{T}"/>
/// <seealso cref="ValueChangedEventArgs{T}"/>
public interface IValue<TValue>
{
    /// <summary>
    ///     Gets or sets the value.
    /// </summary>
    TValue? Value { get; set; }

    /// <summary>
    ///     Raised when <see cref="Value"/> is about to change.
    ///     Set <see cref="ValueChangingEventArgs{T}.Handled"/> to <see langword="true"/> to cancel the change.
    /// </summary>
    event EventHandler<ValueChangingEventArgs<TValue?>>? ValueChanging;

    /// <summary>
    ///     Raised when <see cref="Value"/> has changed.
    /// </summary>
    event EventHandler<ValueChangedEventArgs<TValue?>>? ValueChanged;
}
```

### IValue<TValue> Tests

```csharp
// Tests/UnitTestsParallelizable/ViewBase/IValueTests.cs
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
```

---

## Part 2: AttributePicker Class

### Class Structure

```csharp
using Terminal.Gui.App;

namespace Terminal.Gui.Views;

/// <summary>
///     Allows the user to pick an <see cref="Attribute"/> by selecting foreground and background colors,
///     and text styles.
/// </summary>
public class AttributePicker : View, IValue<Attribute?>, IDesignable
{
    // Backing field immediately before property
    private Attribute? _value;

    // SubViews
    private ColorPicker? _foregroundPicker;
    private ColorPicker? _backgroundPicker;
    private FlagSelector<TextStyle>? _styleSelector;
    private View? _sampleLabel; // Don't use Label as it has extra logic we don't need

    private string _sampleText = "Sample Text";

    // Properties
    public Attribute? Value { get; set; }    // Main selected attribute (IValue<Attribute?>)
    public string SampleText { get; set; }   // Customizable sample text

    // Events (CWP pattern - using CWP event arg types)
    public event EventHandler<ValueChangingEventArgs<Attribute?>>? ValueChanging;
    public event EventHandler<ValueChangedEventArgs<Attribute?>>? ValueChanged;

    // Virtual methods for CWP
    protected virtual bool OnValueChanging (ValueChangingEventArgs<Attribute?> args);
    protected virtual void OnValueChanged (ValueChangedEventArgs<Attribute?> args);

    // Methods
    public bool EnableForDesign();  // IDesignable
}
```

## Layout

```
+----------------------------+--------------------+
| [Foreground ColorPicker]   | [FlagSelector      |
| Title: "Foreground"        |  <TextStyle>]      |
| BorderStyle = Single       | Title: "Style"     |
+----------------------------+ BorderStyle=Single |
| [Background ColorPicker]   |                    |
| Title: "Background"        |                    |
| BorderStyle = Single       |                    |
+----------------------------+--------------------+
|          Sample Text                            |
|     (Rendered with Value attribute)             |
+------------------------------------------------+
```

### Layout Details

- **Left column**: ColorPickers stacked vertically (Width = Dim.Fill(0, 48) - style selector width)
- **Right column**: FlagSelector<TextStyle> (Width = Dim.Auto(), aligned to right)
- **Bottom row**: Sample text centered, full width
- **Use border auto-joining for ColorPickers and FlagSelector** - See AdornmentsEditor for example; set BorderStyle = LineStyle.Single, use SuperViewRendersLineCanvas = true on subviews, and ensure proper positioning to enable auto-joining.
- **Sample text**: Width = Dim.Width(_foregroundPicker) + Dim.Width(_styleSelector), Height = Dim.Auto(DimAutoStyle.Text). Text should be centered horizontally and be "Sample Text" by default.

## Constructor

```csharp
public AttributePicker ()
{
    CanFocus = true;
    TabStop = TabBehavior.TabStop;
    Height = Dim.Auto ();
    Width = Dim.Auto ();

    SetupSubViews ();
}
```

## Implementation Steps

### 1. SetupSubViews Method

```csharp
private void SetupSubViews ()
{
    // Create foreground picker - offset X = -1 for border auto-joining with parent
    _foregroundPicker = new ()
    {
        Title = "Foreground",
        BorderStyle = LineStyle.Single,
        SuperViewRendersLineCanvas = true, // Enable border auto-joining
        X = -1, // Offset to overlap with parent border for auto-join
        Y = 0
    };
    _foregroundPicker.ColorChanged += OnForegroundColorChanged;
    // Remove bottom border thickness for auto-joining with background picker
    _foregroundPicker.Border!.Thickness = _foregroundPicker.Border!.Thickness with { Bottom = 0 };

    // Create background picker - positioned below foreground
    _backgroundPicker = new ()
    {
        Title = "Background",
        BorderStyle = LineStyle.Single,
        SuperViewRendersLineCanvas = true, // Enable border auto-joining
        X = Pos.Left (_foregroundPicker),
        Y = Pos.Bottom (_foregroundPicker),
        Width = Dim.Width (_foregroundPicker)
    };
    _backgroundPicker.ColorChanged += OnBackgroundColorChanged;
    // Remove bottom border for auto-joining with sample label area
    _backgroundPicker.Border!.Thickness = _backgroundPicker.Border!.Thickness with { Bottom = 0 };

    // Create style selector - on the right side
    _styleSelector = new ()
    {
        Title = "Style",
        BorderStyle = LineStyle.Single,
        SuperViewRendersLineCanvas = true, // Enable border auto-joining
        X = Pos.Right (_foregroundPicker) - 1, // Overlap by 1 for border auto-join
        Y = 0,
        Width = Dim.Auto (),
        Height = Dim.Height (_foregroundPicker) + Dim.Height (_backgroundPicker)
    };
    _styleSelector.ValueChanged += OnStyleChanged;

    // Set color picker widths relative to style selector
    _foregroundPicker.Width = Dim.Fill () - Dim.Width (_styleSelector) + 1; // +1 for overlap
    _backgroundPicker.Width = Dim.Width (_foregroundPicker);

    // Create sample label - below the pickers
    _sampleLabel = new ()
    {
        Text = _sampleText,
        Y = Pos.Bottom (_backgroundPicker) - 1,
        X = Pos.Left (_foregroundPicker),
        Width = Dim.Width (_foregroundPicker) + Dim.Width (_styleSelector), // Span full width
        Height = Dim.Auto (DimAutoStyle.Text) + 1, // Support multi-line
        TextFormatter = new () { Alignment = Alignment.Center }
    };

    Add (_foregroundPicker, _backgroundPicker, _styleSelector, _sampleLabel);

    // Set initial value
    _value = Attribute.Default;
    SyncSubViewsToValue ();
    UpdateSampleLabel ();
}
```

**Note on Border Auto-Joining:**
- Views with `SuperViewRendersLineCanvas = true` contribute their borders to the parent's line canvas
- Overlapping positions allow borders to share cells
- Removing bottom border thickness (`Border!.Thickness with { Bottom = 0 }`) prevents double lines
- The LineCanvas automatically renders intersection characters (T-junctions, corners) where borders meet

### 2. Value Property with CWP Pattern (using CWPPropertyHelper)

```csharp
public Attribute? Value
{
    get => _value;
    set => CWPPropertyHelper.ChangeProperty (
        this,
        ref _value,
        value,
        OnValueChanging,
        ValueChanging,
        DoValueChanged, // The work to do after change is confirmed
        OnValueChanged,
        ValueChanged,
        out _);
}

/// <summary>
///     Performs the work after value change is confirmed (sync subviews, update sample).
/// </summary>
private void DoValueChanged (Attribute? newValue)
{
    // Sync subviews (unhook events to prevent recursion)
    SyncSubViewsToValue ();
    UpdateSampleLabel ();
}

/// <summary>
///     Called before <see cref="Value"/> changes. Return <see langword="true"/> to cancel the change.
/// </summary>
protected virtual bool OnValueChanging (ValueChangingEventArgs<Attribute?> args) => false;

/// <summary>
///     Called after <see cref="Value"/> has changed.
/// </summary>
protected virtual void OnValueChanged (ValueChangedEventArgs<Attribute?> args) { }
```

**CWPPropertyHelper.ChangeProperty Flow:**
1. Checks if newValue equals currentValue (early return if same)
2. Creates ValueChangingEventArgs and calls OnValueChanging (can return true to cancel)
3. Raises ValueChanging event (handlers can set Handled = true to cancel)
4. Calls doWork action (DoValueChanged) to perform the actual work
5. Updates the backing field
6. Creates ValueChangedEventArgs and calls OnValueChanged
7. Raises ValueChanged event

### 3. Event Handlers

```csharp
private void OnForegroundColorChanged (object? sender, ResultEventArgs<Color> e)
{
    UpdateValueFromSubViews ();
}

private void OnBackgroundColorChanged (object? sender, ResultEventArgs<Color> e)
{
    UpdateValueFromSubViews ();
}

private void OnStyleChanged (object? sender, EventArgs<TextStyle?> e)
{
    UpdateValueFromSubViews ();
}

private void UpdateValueFromSubViews ()
{
    if (_foregroundPicker is null || _backgroundPicker is null || _styleSelector is null)
    {
        return;
    }

    Attribute newValue = new (
        _foregroundPicker.SelectedColor,
        _backgroundPicker.SelectedColor,
        _styleSelector.Value ?? TextStyle.None
    );
    SetValue (newValue);
}
```

### 4. Synchronization Methods

```csharp
private void SyncSubViewsToValue ()
{
    if (!_value.HasValue)
    {
        return;
    }

    // Temporarily unhook events to prevent recursion
    if (_foregroundPicker is { })
    {
        _foregroundPicker.ColorChanged -= OnForegroundColorChanged;
        _foregroundPicker.SelectedColor = _value.Value.Foreground;
        _foregroundPicker.ColorChanged += OnForegroundColorChanged;
    }

    if (_backgroundPicker is { })
    {
        _backgroundPicker.ColorChanged -= OnBackgroundColorChanged;
        _backgroundPicker.SelectedColor = _value.Value.Background;
        _backgroundPicker.ColorChanged += OnBackgroundColorChanged;
    }

    if (_styleSelector is { })
    {
        _styleSelector.ValueChanged -= OnStyleChanged;
        _styleSelector.Value = _value.Value.Style;
        _styleSelector.ValueChanged += OnStyleChanged;
    }
}

private void UpdateSampleLabel ()
{
    if (_sampleLabel is null || !_value.HasValue)
    {
        return;
    }

    _sampleLabel.Text = _sampleText;

    // Create Scheme with Value for Normal role
    Scheme sampleScheme = new (_value.Value);
    _sampleLabel.SetScheme (sampleScheme);

    SetNeedsLayout ();
}
```

### 5. SampleText Property

```csharp
public string SampleText
{
    get => _sampleText;
    set
    {
        if (_sampleText != value)
        {
            _sampleText = value;
            UpdateSampleLabel ();
        }
    }
}
```

### 6. IDesignable Implementation

```csharp
public bool EnableForDesign ()
{
    SampleText = "Multi-line Sample Text.\nThis is the second line.";
    Value = new (Color.BrightRed, Color.DarkBlue, TextStyle.Bold | TextStyle.Underline);
    return true;
}
```

### 7. Dispose Override

```csharp
protected override void Dispose (bool disposing)
{
    if (disposing)
    {
        if (_foregroundPicker is { })
        {
            _foregroundPicker.ColorChanged -= OnForegroundColorChanged;
        }

        if (_backgroundPicker is { })
        {
            _backgroundPicker.ColorChanged -= OnBackgroundColorChanged;
        }

        if (_styleSelector is { })
        {
            _styleSelector.ValueChanged -= OnStyleChanged;
        }
    }

    base.Dispose (disposing);
}
```

## TextStyle Enum Values

The `FlagSelector<TextStyle>` will automatically display these options from the `[Flags]` enum:

- **None** - No text style
- **Bold** - Bold text (SGR 1)
- **Faint** - Dim text (SGR 2)
- **Italic** - Italic text (SGR 3)
- **Underline** - Underlined text (SGR 4)
- **Blink** - Blinking text (SGR 5)
- **Reverse** - Swaps foreground/background (SGR 7)
- **Strikethrough** - Crossed-out text (SGR 9)

## Verification Plan

1. **Build**: `dotnet build Terminal.Gui/Terminal.Gui.csproj`
2. **Unit Tests**: `dotnet test Tests/UnitTestsParallelizable --filter "FullyQualifiedName~AttributePicker"`
3. **Manual Test**: Add to UICatalog ColorPicker scenario or create simple test app

## Unit Tests

```csharp
// Tests/UnitTestsParallelizable/Views/AttributePickerTests.cs
// Comment: // Claude - Opus 4.5

// IValue<Attribute?> interface tests
[Fact] Implements_IValue_Interface()
[Fact] Constructor_SetsDefaultValue()
[Fact] ValueChanged_Fires_WhenValueChanges()
[Fact] ValueChanged_ReceivesOldAndNewValue()
[Fact] ValueChanging_CanCancel_ViaHandled()
[Fact] ValueChanging_Fires_BeforeValueChanged()
[Fact] Value_DoesNotChange_WhenCancelled()

// Subview synchronization tests
[Fact] Value_UpdatesSampleLabel()
[Fact] ForegroundPicker_ChangeUpdatesValue()
[Fact] BackgroundPicker_ChangeUpdatesValue()
[Fact] StyleSelector_ChangeUpdatesValue()
[Fact] Value_IncludesTextStyle()
[Fact] Setting_Value_Updates_AllSubViews()

// Other tests
[Fact] SampleText_Property_UpdatesLabel()
[Fact] EnableForDesign_ReturnsTrue()
[Fact] Dispose_UnhooksEventHandlers()
```

## Code Style Reminders

- NO `var` except for built-in types
- Use `new ()` not `new TypeName()`
- Use `[...]` for collections
- Unused lambda params: `(_, _) => {...}`
- Backing fields immediately before properties
- SubView/SuperView terminology

## Critical Files Reference

- `Terminal.Gui/App/CWP/CWPPropertyHelper.cs` - CWP helper for property changes
- `Terminal.Gui/App/CWP/ValueChangingEventArgs.cs` - Pre-change event args (CWP)
- `Terminal.Gui/App/CWP/ValueChangedEventArgs.cs` - Post-change event args (CWP)
- `Terminal.Gui/Views/Color/ColorPicker.cs` - Pattern for structure, events
- `Terminal.Gui/Views/Selectors/FlagSelectorTEnum.cs` - Generic FlagSelector<T>
- `Terminal.Gui/Drawing/Attribute.cs` - The Attribute struct
- `Terminal.Gui/Drawing/TextStyle.cs` - The TextStyle flags enum
- `Terminal.Gui/Drawing/Scheme.cs` - For setting sample label appearance
- `Examples/UICatalog/Scenarios/EditorsAndHelpers/AdornmentsEditor.cs` - Example of border auto-joining
