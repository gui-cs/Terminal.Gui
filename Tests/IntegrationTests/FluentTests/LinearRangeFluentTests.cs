using TerminalGuiFluentTesting;
using Xunit.Abstractions;

namespace IntegrationTests.FluentTests;

public class LinearRangeFluentTests (ITestOutputHelper outputHelper)
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void LinearRange_CanCreateAndRender (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Add (
                                           new LinearRange<int> (new() { 0, 10, 20, 30, 40, 50 })
                                           {
                                               X = 2,
                                               Y = 2,
                                               Type = LinearRangeType.Single
                                           })
                                     .Focus<LinearRange<int>> ()
                                     .WaitIteration ()
                                     .ScreenShot ("LinearRange initial render", _out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void LinearRange_CanNavigateWithArrowKeys (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Add (
                                           new LinearRange<int> (new() { 0, 10, 20, 30 })
                                           {
                                               X = 2,
                                               Y = 2,
                                               Type = LinearRangeType.Single,
                                               AllowEmpty = false
                                           })
                                     .Focus<LinearRange<int>> ()
                                     .WaitIteration ()
                                     .ScreenShot ("Initial state", _out)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .WaitIteration ()
                                     .ScreenShot ("After right arrow", _out)
                                     .EnqueueKeyEvent (Key.CursorRight)
                                     .WaitIteration ()
                                     .ScreenShot ("After second right arrow", _out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void LinearRange_TypeChange_TriggersEvents (TestDriver d)
    {
        LinearRange<int> linearRange = new (new() { 0, 10, 20, 30 })
        {
            X = 2,
            Y = 2,
            Type = LinearRangeType.Single
        };

        var changingEventRaised = false;
        var changedEventRaised = false;

        linearRange.TypeChanging += (_, args) =>
                                    {
                                        changingEventRaised = true;
                                        Assert.Equal (LinearRangeType.Single, args.CurrentValue);
                                        Assert.Equal (LinearRangeType.Range, args.NewValue);
                                    };

        linearRange.TypeChanged += (_, args) =>
                                   {
                                       changedEventRaised = true;
                                       Assert.Equal (LinearRangeType.Single, args.OldValue);
                                       Assert.Equal (LinearRangeType.Range, args.NewValue);
                                   };

        // Change the type before adding to window
        linearRange.Type = LinearRangeType.Range;

        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Add (linearRange)
                                     .Focus<LinearRange<int>> ()
                                     .WaitIteration ()
                                     .ScreenShot ("After type change to Range", _out)
                                     .Stop ();

        Assert.True (changingEventRaised);
        Assert.True (changedEventRaised);
        Assert.Equal (LinearRangeType.Range, linearRange.Type);
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void LinearRange_RangeType_CanSelectRange (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Add (
                                           new LinearRange<int> (new() { 0, 10, 20, 30, 40 })
                                           {
                                               X = 2,
                                               Y = 2,
                                               Type = LinearRangeType.Range,
                                               AllowEmpty = false
                                           })
                                     .Focus<LinearRange<int>> ()
                                     .WaitIteration ()
                                     .ScreenShot ("Range type initial", _out)
                                     .EnqueueKeyEvent (Key.Space)
                                     .WaitIteration ()
                                     .ScreenShot ("After first selection", _out)
                                     .EnqueueKeyEvent (Key.CursorRight.WithCtrl)
                                     .WaitIteration ()
                                     .EnqueueKeyEvent (Key.CursorRight.WithCtrl)
                                     .WaitIteration ()
                                     .ScreenShot ("After extending range", _out)
                                     .Stop ();
    }

    [Theory]
    [ClassData (typeof (TestDrivers))]
    public void LinearRange_VerticalOrientation_Renders (TestDriver d)
    {
        using GuiTestContext c = With.A<Window> (80, 25, d, _out)
                                     .Add (
                                           new LinearRange<int> (new() { 0, 10, 20, 30 })
                                           {
                                               X = 2,
                                               Y = 2,
                                               Orientation = Orientation.Vertical,
                                               Type = LinearRangeType.Single
                                           })
                                     .Focus<LinearRange<int>> ()
                                     .WaitIteration ()
                                     .ScreenShot ("Vertical orientation", _out)
                                     .EnqueueKeyEvent (Key.CursorDown)
                                     .WaitIteration ()
                                     .ScreenShot ("After down arrow", _out)
                                     .Stop ();
    }
}
