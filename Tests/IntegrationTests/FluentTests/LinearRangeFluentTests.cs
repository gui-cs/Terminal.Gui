using AppTestHelpers;

namespace IntegrationTests;

public class LinearRangeFluentTests (ITestOutputHelper outputHelper) : TestsAllDrivers
{
    private readonly TextWriter _out = new TestOutputWriter (outputHelper);

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_CanCreateAndRender (string d)
    {
        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (
                                              new LinearRange<int> ([0, 10, 20, 30, 40, 50])
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
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_CanNavigateWithArrowKeys (string d)
    {
        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (
                                              new LinearRange<int> ([0, 10, 20, 30])
                                              {
                                                  X = 2,
                                                  Y = 2,
                                                  Type = LinearRangeType.Single,
                                                  AllowEmpty = false
                                              })
                                        .Focus<LinearRange<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("Initial state", _out)
                                        .KeyDown (Key.CursorRight)
                                        .WaitIteration ()
                                        .ScreenShot ("After right arrow", _out)
                                        .KeyDown (Key.CursorRight)
                                        .WaitIteration ()
                                        .ScreenShot ("After second right arrow", _out)
                                        .Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_TypeChange_TriggersEvents (string d)
    {
        LinearRange<int> linearRange = new ([0, 10, 20, 30])
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

        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
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
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_RangeType_CanSelectRange (string d)
    {
        using AppTestHelper c = With.A<Window> (30, 10, d, _out)
                                        .Add (
                                              new LinearRange<int> ([0, 10, 20, 30, 40])
                                              {
                                                  X = 2,
                                                  Y = 2,
                                                  Type = LinearRangeType.Range,
                                                  AllowEmpty = false
                                              })
                                        .Focus<LinearRange<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("Range type initial", _out)
                                        .KeyDown (Key.Space)
                                        .WaitIteration ()
                                        .ScreenShot ("After first selection", _out)
                                        .KeyDown (Key.CursorRight.WithCtrl)
                                        .WaitIteration ()
                                        .KeyDown (Key.CursorRight.WithCtrl)
                                        .WaitIteration ()
                                        .ScreenShot ("After extending range", _out)
                                        .Stop ();
    }

    [Theory]
    [MemberData (nameof (GetAllDriverNames))]
    public void LinearRange_VerticalOrientation_Renders (string d)
    {
        using AppTestHelper c = With.A<Window> (10, 15, d, _out)
                                        .Add (
                                              new LinearRange<int> ([0, 10, 20, 30])
                                              {
                                                  X = 2,
                                                  Y = 2,
                                                  Orientation = Orientation.Vertical,
                                                  Type = LinearRangeType.Single
                                              })
                                        .Focus<LinearRange<int>> ()
                                        .WaitIteration ()
                                        .ScreenShot ("Vertical orientation", _out)
                                        .KeyDown (Key.CursorDown)
                                        .WaitIteration ()
                                        .ScreenShot ("After down arrow", _out)
                                        .Stop ();
    }
}
