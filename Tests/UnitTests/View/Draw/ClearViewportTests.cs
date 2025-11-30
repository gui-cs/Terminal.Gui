#nullable enable
using Moq;
using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewTests;

[Trait ("Category", "Output")]
public class ClearViewportTests (ITestOutputHelper output)
{
    public class TestableView : View
    {
        public bool TestOnClearingViewport () { return OnClearingViewport (); }

        public int OnClearingViewportCalled { get; set; }
        public bool CancelOnClearingViewport { get; set; }

        protected override bool OnClearingViewport ()
        {
            OnClearingViewportCalled++;

            return CancelOnClearingViewport;
        }

        public int OnClearedViewportCalled { get; set; }
        protected override void OnClearedViewport () { OnClearedViewportCalled++; }
    }

    [Fact]
    public void DoClearViewport_ViewportIsTransparent_DoesNotClear ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        view.Object.ViewportSettings = ViewportSettingsFlags.Transparent;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (0, view.Object.OnClearingViewportCalled);
        Assert.Equal (0, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_OnClearingViewportReturnsTrue_DoesNotClear ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        view.Object.CancelOnClearingViewport = true;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (0, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_ClearingViewportEventCancelled_DoesNotClear ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        view.Object.ClearingViewport += (sender, e) => e.Cancel = true;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (0, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_ClearsViewport ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.Equal (1, view.Object.OnClearedViewportCalled);
    }

    [Fact]
    public void DoClearViewport_RaisesClearingViewportEvent ()
    {
        // Arrange
        Mock<TestableView> view = new () { CallBase = true };
        var eventRaised = false;
        view.Object.ClearingViewport += (sender, e) => eventRaised = true;

        // Act
        view.Object.DoClearViewport ();

        // Assert
        Assert.True (eventRaised);
    }

    [Fact]
    [SetupFakeApplication]
    public void Clear_ClearsEntireViewport ()
    {
        var superView = new View
        {
            App = ApplicationImpl.Instance,
            Width = Dim.Fill (), Height = Dim.Fill ()
        };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();
        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       output);

        // On Draw exit the view is excluded from the clip, so this will do nothing.
        view.ClearViewport ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       output);

       view.SetClipToScreen ();

        view.ClearViewport ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       output);
    }

    [Fact]
    [SetupFakeApplication]
    public void Clear_WithClearVisibleContentOnly_ClearsVisibleContentOnly ()
    {
        var superView = new View
        {
            App = ApplicationImpl.Instance,
            Width = Dim.Fill (), Height = Dim.Fill ()
        };

        var view = new View
        {
            Text = "X",
            X = 1, Y = 1,
            Width = 3, Height = 3,
            BorderStyle = LineStyle.Single,
            ViewportSettings = ViewportSettingsFlags.ClearContentOnly
        };
        superView.Add (view);
        superView.BeginInit ();
        superView.EndInit ();
        superView.LayoutSubViews ();

        superView.Draw ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │X│
 └─┘",
                                                       output);
       view.SetClipToScreen ();
        view.ClearViewport ();

        DriverAssert.AssertDriverContentsWithFrameAre (
                                                       @"
 ┌─┐
 │ │
 └─┘",
                                                       output);
    }

    [Fact]
    [AutoInitShutdown]
    public void Clear_Viewport_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
    {
        var view = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };

        view.DrawingContent += (s, e) =>
                               {
                                   Region? savedClip = view.AddViewportToClip ();

                                   for (var row = 0; row < view.Viewport.Height; row++)
                                   {
                                       Application.Driver?.Move (1, row + 1);

                                       for (var col = 0; col < view.Viewport.Width; col++)
                                       {
                                           Application.Driver?.AddStr ($"{col}");
                                       }
                                   }

                                   view.SetClip (savedClip);
                                   e.Cancel = true;
                               };
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);
        Application.Driver!.SetScreenSize (20, 10);
        Application.LayoutAndDraw ();

        var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
"
            ;

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 20, 10), pos);

        view.FillRect (view.Viewport);

        expected = @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
"
            ;

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Clear_Can_Use_Driver_AddRune_Or_AddStr_Methods ()
    {
        var view = new FrameView { Width = Dim.Fill (), Height = Dim.Fill (), BorderStyle = LineStyle.Single };

        view.DrawingContent += (s, e) =>
                               {
                                   Region? savedClip = view.AddViewportToClip ();

                                   for (var row = 0; row < view.Viewport.Height; row++)
                                   {
                                       Application.Driver?.Move (1, row + 1);

                                       for (var col = 0; col < view.Viewport.Width; col++)
                                       {
                                           Application.Driver?.AddStr ($"{col}");
                                       }
                                   }

                                   view.SetClip (savedClip);
                                   e.Cancel = true;
                               };
        var top = new Toplevel ();
        top.Add (view);
        Application.Begin (top);
        Application.Driver!.SetScreenSize (20, 10);
        Application.LayoutAndDraw ();

        var expected = @"
┌──────────────────┐
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
│012345678910111213│
└──────────────────┘
"
            ;

        Rectangle pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);
        Assert.Equal (new (0, 0, 20, 10), pos);

        view.FillRect (view.Viewport);

        expected = @"
┌──────────────────┐
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
│                  │
└──────────────────┘
";

        pos = DriverAssert.AssertDriverContentsWithFrameAre (expected, output);

        top.Dispose ();
    }
}
