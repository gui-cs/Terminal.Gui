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
        LinearRangeOption<int> o = new ("1 thousand", new ('y'), 1000);
        Assert.Equal ("1 thousand", o.Legend);
        Assert.Equal (new ('y'), o.LegendAbbr);
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
        LinearRangeOption<int> sliderOption = new () { Legend = "Lord flibble", LegendAbbr = new ('l'), Data = 1 };

        Assert.Equal ("{Legend=Lord flibble, LegendAbbr=l, Data=1}", sliderOption.ToString ());
    }

    [Fact]
    public void LinearRangeOption_ToString_WhenPopulated_WithSizeF ()
    {
        LinearRangeOption<SizeF> sliderOption = new () { Legend = "Lord flibble", LegendAbbr = new ('l'), Data = new (32, 11) };

        Assert.Equal ("{Legend=Lord flibble, LegendAbbr=l, Data={Width=32, Height=11}}", sliderOption.ToString ());
    }

    [Fact]
    public void OnChanged_Should_Raise_ChangedEvent ()
    {
        // Arrange
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.Changed += (sender, args) => eventRaised = true;

        // Act
        sliderOption.OnChanged (true);

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    public void OnSet_Should_Raise_SetEvent ()
    {
        // Arrange
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.Set += (sender, args) => eventRaised = true;

        // Act
        sliderOption.OnSet ();

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    public void OnUnSet_Should_Raise_UnSetEvent ()
    {
        // Arrange
        LinearRangeOption<int> sliderOption = new ();
        var eventRaised = false;
        sliderOption.UnSet += (sender, args) => eventRaised = true;

        // Act
        sliderOption.OnUnSet ();

        // Assert
        Assert.True (eventRaised);
    }
}

public class LinearRangeEventArgsTests : TestDriverBase
{
    [Fact]
    public void Constructor_Sets_Cancel_Default_To_False ()
    {
        // Arrange
        Dictionary<int, LinearRangeOption<int>> options = new ();
        var focused = 42;

        // Act
        LinearRangeEventArgs<int> sliderEventArgs = new (options, focused);

        // Assert
        Assert.False (sliderEventArgs.Cancel);
    }

    [Fact]
    public void Constructor_Sets_Focused ()
    {
        // Arrange
        Dictionary<int, LinearRangeOption<int>> options = new ();
        var focused = 42;

        // Act
        LinearRangeEventArgs<int> sliderEventArgs = new (options, focused);

        // Assert
        Assert.Equal (focused, sliderEventArgs.Focused);
    }

    [Fact]
    public void Constructor_Sets_Options ()
    {
        // Arrange
        Dictionary<int, LinearRangeOption<int>> options = new ();

        // Act
        LinearRangeEventArgs<int> sliderEventArgs = new (options);

        // Assert
        Assert.Equal (options, sliderEventArgs.Options);
    }
}

public class LinearRangeTests : TestDriverBase
{
    [Fact]
    public void Constructor_Default ()
    {
        // Arrange & Act
        LinearRange<int> slider = new ();

        // Assert
        Assert.NotNull (slider);
        Assert.NotNull (slider.Options);
        Assert.Empty (slider.Options);
        Assert.Equal (Orientation.Horizontal, slider.Orientation);
        Assert.False (slider.AllowEmpty);
        Assert.True (slider.ShowLegends);
        Assert.False (slider.ShowEndSpacing);
        Assert.Equal (LinearRangeType.Single, slider.Type);
        Assert.Equal (1, slider.MinimumInnerSpacing);
        Assert.True (slider.Width is DimAuto);
        Assert.True (slider.Height is DimAuto);
        Assert.Equal (0, slider.FocusedOption);
    }

    [Fact]
    public void Constructor_With_Options ()
    {
        // Arrange
        List<int> options = new () { 1, 2, 3 };

        // Act
        LinearRange<int> slider = new (options);
        slider.SetRelativeLayout (new (100, 100));

        // Assert
        // 0123456789
        // 1 2 3
        Assert.Equal (1, slider.MinimumInnerSpacing);
        Assert.Equal (new (5, 2), slider.GetContentSize ());
        Assert.Equal (new (5, 2), slider.Frame.Size);
        Assert.NotNull (slider);
        Assert.NotNull (slider.Options);
        Assert.Equal (options.Count, slider.Options.Count);
    }

    [Fact]
    public void MovePlus_Should_MoveFocusRight_When_OptionIsAvailable ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });

        // Act
        bool result = slider.MovePlus ();

        // Assert
        Assert.True (result);
        Assert.Equal (1, slider.FocusedOption);
    }

    [Fact]
    public void MovePlus_Should_NotMoveFocusRight_When_AtEnd ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });

        slider.FocusedOption = 3;

        // Act
        bool result = slider.MovePlus ();

        // Assert
        Assert.False (result);
        Assert.Equal (3, slider.FocusedOption);
    }

    [Fact]
    public void OnOptionFocused_Event_Cancelled ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3 });
        var eventRaised = false;
        var cancel = false;
        slider.OptionFocused += (sender, args) => eventRaised = true;
        var newFocusedOption = 1;

        // Create args with cancel set to false
        cancel = false;

        LinearRangeEventArgs<int> args = new (new (), newFocusedOption) { Cancel = cancel };
        Assert.Equal (0, slider.FocusedOption);

        // Act
        slider.OnOptionFocused (newFocusedOption, args);

        // Assert
        Assert.True (eventRaised); // Event should be raised
        Assert.Equal (newFocusedOption, slider.FocusedOption); // Focused option should change

        // Create args with cancel set to true
        cancel = true;

        args = new (new (), newFocusedOption) { Cancel = cancel };

        // Act
        slider.OnOptionFocused (2, args);

        // Assert
        Assert.True (eventRaised); // Event should be raised
        Assert.Equal (newFocusedOption, slider.FocusedOption); // Focused option should not change
    }

    [Fact]
    public void OnOptionFocused_Event_Raised ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3 });
        var eventRaised = false;
        slider.OptionFocused += (sender, args) => eventRaised = true;
        var newFocusedOption = 1;
        LinearRangeEventArgs<int> args = new (new (), newFocusedOption);

        // Act
        slider.OnOptionFocused (newFocusedOption, args);

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    public void OnOptionsChanged_Event_Raised ()
    {
        // Arrange
        LinearRange<int> slider = new ();
        var eventRaised = false;
        slider.OptionsChanged += (sender, args) => eventRaised = true;

        // Act
        slider.OnOptionsChanged ();

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    public void Set_Should_Not_UnSetFocusedOption_When_EmptyNotAllowed ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 }) { AllowEmpty = false };

        Assert.NotEmpty (slider.GetSetOptions ());

        // Act
        bool result = slider.UnSetOption (slider.FocusedOption);

        // Assert
        Assert.False (result);
        Assert.NotEmpty (slider.GetSetOptions ());
    }

    // Add similar tests for other methods like MoveMinus, MoveStart, MoveEnd, Set, etc.

    [Fact]
    public void Set_Should_SetFocusedOption ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });

        // Act
        slider.FocusedOption = 2;
        bool result = slider.Select ();

        // Assert
        Assert.True (result);
        Assert.Equal (2, slider.FocusedOption);
        Assert.Single (slider.GetSetOptions ());
    }

    [Fact]
    public void TryGetOptionByPosition_InvalidPosition_Failure ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3 });
        var x = 10;
        var y = 10;
        var threshold = 2;
        int expectedOption = -1;

        // Act
        bool result = slider.TryGetOptionByPosition (x, y, threshold, out int option);

        // Assert
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
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });

        // 0123456789
        // 1234

        slider.MinimumInnerSpacing = 2;

        // 0123456789
        // 1--2--3--4

        // Arrange

        // Act
        bool result = slider.TryGetOptionByPosition (x, y, threshold, out int option);

        // Assert
        Assert.True (result);
        Assert.Equal (expectedData, slider.Options [option].Data);
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
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });
        slider.Orientation = Orientation.Vertical;

        // Set auto size to true to enable testing
        slider.MinimumInnerSpacing = 2;

        // 0 1
        // 1 |
        // 2 |
        // 3 2
        // 4 |
        // 5 |
        // 6 3
        // 7 |
        // 8 |
        // 9 4

        // Act
        bool result = slider.TryGetOptionByPosition (x, y, threshold, out int option);

        // Assert
        Assert.True (result);
        Assert.Equal (expectedData, slider.Options [option].Data);
    }

    [Fact]
    public void TryGetPositionByOption_InvalidOption_Failure ()
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3 });
        int option = -1;
        (int, int) expectedPosition = (-1, -1);

        // Act
        bool result = slider.TryGetPositionByOption (option, out (int x, int y) position);

        // Assert
        Assert.False (result);
        Assert.Equal (expectedPosition, position);
    }

    [Theory]
    [InlineData (0, 0, 0)]
    [InlineData (1, 3, 0)]
    [InlineData (3, 9, 0)]
    public void TryGetPositionByOption_ValidOptionHorizontal_Success (int option, int expectedX, int expectedY)
    {
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });

        // Set auto size to true to enable testing
        slider.MinimumInnerSpacing = 2;

        // 0123456789
        // 1--2--3--4

        // Act
        bool result = slider.TryGetPositionByOption (option, out (int x, int y) position);

        // Assert
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
        // Arrange
        LinearRange<int> slider = new (new () { 1, 2, 3, 4 });
        slider.Orientation = Orientation.Vertical;

        // Set auto size to true to enable testing
        slider.MinimumInnerSpacing = 2;

        // Act
        bool result = slider.TryGetPositionByOption (option, out (int x, int y) position);

        // Assert
        Assert.True (result);
        Assert.Equal (expectedX, position.x);
        Assert.Equal (expectedY, position.y);
    }

    [Fact]
    private void DimAuto_Both_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = Dim.Fill (), Height = Dim.Fill () };

        List<object> options = ["01234", "01234"];

        LinearRange slider = new (options) { Orientation = Orientation.Vertical, Type = LinearRangeType.Multiple };
        view.Add (slider);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = slider.Frame.Size;

        Assert.Equal (new (6, 3), expectedSize);

        view.SetContentSize (new (1, 1));

        view.LayoutSubViews ();
        slider.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, slider.Frame.Size);
    }

    [Fact]
    private void DimAuto_Height_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = 10, Height = Dim.Fill () };

        List<object> options = new () { "01234", "01234" };

        LinearRange slider = new (options) { Orientation = Orientation.Vertical, Type = LinearRangeType.Multiple, Width = 10 };
        view.Add (slider);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = slider.Frame.Size;

        Assert.Equal (new (10, 3), expectedSize);

        view.SetContentSize (new (1, 1));

        view.LayoutSubViews ();
        slider.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, slider.Frame.Size);
    }

    [Fact]
    private void DimAuto_Width_Respects_SuperView_ContentSize ()
    {
        View view = new () { Width = Dim.Fill (), Height = 10 };

        List<object> options = new () { "01234", "01234" };

        LinearRange slider = new (options) { Orientation = Orientation.Vertical, Type = LinearRangeType.Multiple, Height = 10 };
        view.Add (slider);
        view.BeginInit ();
        view.EndInit ();

        Size expectedSize = slider.Frame.Size;

        Assert.Equal (new (6, 10), expectedSize);

        view.SetContentSize (new (1, 1));

        view.LayoutSubViews ();
        slider.SetRelativeLayout (view.Viewport.Size);

        Assert.Equal (expectedSize, slider.Frame.Size);
    }

    // https://github.com/gui-cs/Terminal.Gui/issues/3099
    [Fact]
    private void One_Option_Does_Not_Throw ()
    {
        // Arrange
        LinearRange<int> slider = new ();
        slider.BeginInit ();
        slider.EndInit ();

        // Act/Assert
        slider.Options = [new ()];
    }

    // Add more tests for different scenarios and edge cases.
}

public class LinearRangeCWPTests : TestDriverBase
{
    [Fact]
    public void LegendsOrientation_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = Orientation.Horizontal;
        var newValue = Orientation.Vertical;

        linearRange.LegendsOrientationChanging += (sender, args) =>
                                                  {
                                                      changingRaised = true;
                                                      Assert.Equal (oldValue, args.CurrentValue);
                                                      Assert.Equal (newValue, args.NewValue);
                                                  };

        linearRange.LegendsOrientationChanged += (sender, args) =>
                                                 {
                                                     changedRaised = true;
                                                     Assert.Equal (oldValue, args.OldValue);
                                                     Assert.Equal (newValue, args.NewValue);
                                                 };

        // Act
        linearRange.LegendsOrientation = newValue;

        // Assert
        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, linearRange.LegendsOrientation);
    }

    [Fact]
    public void MinimumInnerSpacing_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = 1;
        var newValue = 5;

        linearRange.MinimumInnerSpacingChanging += (sender, args) =>
                                                   {
                                                       changingRaised = true;
                                                       Assert.Equal (oldValue, args.CurrentValue);
                                                       Assert.Equal (newValue, args.NewValue);
                                                   };

        linearRange.MinimumInnerSpacingChanged += (sender, args) =>
                                                  {
                                                      changedRaised = true;
                                                      Assert.Equal (oldValue, args.OldValue);
                                                      Assert.Equal (newValue, args.NewValue);
                                                  };

        // Act
        linearRange.MinimumInnerSpacing = newValue;

        // Assert
        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, linearRange.MinimumInnerSpacing);
    }

    [Fact]
    public void ShowEndSpacing_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = false;
        var newValue = true;

        linearRange.ShowEndSpacingChanging += (sender, args) =>
                                              {
                                                  changingRaised = true;
                                                  Assert.Equal (oldValue, args.CurrentValue);
                                                  Assert.Equal (newValue, args.NewValue);
                                              };

        linearRange.ShowEndSpacingChanged += (sender, args) =>
                                             {
                                                 changedRaised = true;
                                                 Assert.Equal (oldValue, args.OldValue);
                                                 Assert.Equal (newValue, args.NewValue);
                                             };

        // Act
        linearRange.ShowEndSpacing = newValue;

        // Assert
        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, linearRange.ShowEndSpacing);
    }

    [Fact]
    public void ShowLegends_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = true;
        var newValue = false;

        linearRange.ShowLegendsChanging += (sender, args) =>
                                           {
                                               changingRaised = true;
                                               Assert.Equal (oldValue, args.CurrentValue);
                                               Assert.Equal (newValue, args.NewValue);
                                           };

        linearRange.ShowLegendsChanged += (sender, args) =>
                                          {
                                              changedRaised = true;
                                              Assert.Equal (oldValue, args.OldValue);
                                              Assert.Equal (newValue, args.NewValue);
                                          };

        // Act
        linearRange.ShowLegends = newValue;

        // Assert
        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, linearRange.ShowLegends);
    }

    [Fact]
    public void Type_PropertyChange_CanBeCancelled ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        LinearRangeType oldValue = linearRange.Type;

        linearRange.TypeChanging += (sender, args) => { args.Handled = true; };

        // Act
        linearRange.Type = LinearRangeType.Range;

        // Assert
        Assert.Equal (oldValue, linearRange.Type);
    }

    [Fact]
    public void Type_PropertyChange_ChangingEventCanModifyNewValue ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var modifiedValue = LinearRangeType.Multiple;

        linearRange.TypeChanging += (sender, args) => { args.NewValue = modifiedValue; };

        // Act
        linearRange.Type = LinearRangeType.Range;

        // Assert
        Assert.Equal (modifiedValue, linearRange.Type);
    }

    [Fact]
    public void Type_PropertyChange_NoEventsWhenValueUnchanged ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;

        linearRange.TypeChanging += (sender, args) => changingRaised = true;
        linearRange.TypeChanged += (sender, args) => changedRaised = true;

        // Act
        linearRange.Type = linearRange.Type;

        // Assert
        Assert.False (changingRaised);
        Assert.False (changedRaised);
    }

    [Fact]
    public void Type_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = LinearRangeType.Single;
        var newValue = LinearRangeType.Range;

        linearRange.TypeChanging += (sender, args) =>
                                    {
                                        changingRaised = true;
                                        Assert.Equal (oldValue, args.CurrentValue);
                                        Assert.Equal (newValue, args.NewValue);
                                    };

        linearRange.TypeChanged += (sender, args) =>
                                   {
                                       changedRaised = true;
                                       Assert.Equal (oldValue, args.OldValue);
                                       Assert.Equal (newValue, args.NewValue);
                                   };

        // Act
        linearRange.Type = newValue;

        // Assert
        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, linearRange.Type);
    }

    [Fact]
    public void UseMinimumSize_PropertyChange_RaisesChangingAndChangedEvents ()
    {
        // Arrange
        LinearRange<int> linearRange = new ();
        var changingRaised = false;
        var changedRaised = false;
        var oldValue = false;
        var newValue = true;

        linearRange.UseMinimumSizeChanging += (sender, args) =>
                                              {
                                                  changingRaised = true;
                                                  Assert.Equal (oldValue, args.CurrentValue);
                                                  Assert.Equal (newValue, args.NewValue);
                                              };

        linearRange.UseMinimumSizeChanged += (sender, args) =>
                                             {
                                                 changedRaised = true;
                                                 Assert.Equal (oldValue, args.OldValue);
                                                 Assert.Equal (newValue, args.NewValue);
                                             };

        // Act
        linearRange.UseMinimumSize = newValue;

        // Assert
        Assert.True (changingRaised);
        Assert.True (changedRaised);
        Assert.Equal (newValue, linearRange.UseMinimumSize);
    }
}
