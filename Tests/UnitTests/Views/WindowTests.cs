using UnitTests;
using Xunit.Abstractions;

namespace UnitTests.ViewsTests;

public class WindowTests ()
{
    [Fact]
    public void New_Initializes ()
    {
        // Parameterless
        using var defaultWindow = new Window ();
        defaultWindow.Layout ();
        Assert.NotNull (defaultWindow);
        Assert.Equal (string.Empty, defaultWindow.Title);

        // Runnables have Width/Height set to Dim.Fill

        // If there's no SuperView, Top, or Driver, the default Fill width is int.MaxValue
        Assert.Equal ($"Window(){defaultWindow.Frame}", defaultWindow.ToString ());
        Assert.True (defaultWindow.CanFocus);
        Assert.False (defaultWindow.HasFocus);
        Assert.Equal (new Rectangle (0, 0, Application.Screen.Width - 2, Application.Screen.Height - 2), defaultWindow.Viewport);
        Assert.Equal (new Rectangle (0, 0, Application.Screen.Width, Application.Screen.Height), defaultWindow.Frame);
        Assert.Null (defaultWindow.Focused);
        Assert.NotNull (defaultWindow.GetScheme ());
        Assert.Equal (0, defaultWindow.X);
        Assert.Equal (0, defaultWindow.Y);
        Assert.Equal (Dim.Fill (), defaultWindow.Width);
        Assert.Equal (Dim.Fill (), defaultWindow.Height);
        Assert.False (defaultWindow.IsCurrentTop);
        Assert.Empty (defaultWindow.Id);
        Assert.False (defaultWindow.MouseHoldRepeat);
        Assert.False (defaultWindow.MousePositionTracking );
        Assert.Null (defaultWindow.SuperView);
        Assert.Null (defaultWindow.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, defaultWindow.TextDirection);

        Assert.Equal (ViewArrangement.Overlapped, defaultWindow.Arrangement);

        // Empty Rect
        using var windowWithFrameRectEmpty = new Window { Frame = Rectangle.Empty, Title = "title" };
        windowWithFrameRectEmpty.Layout ();
        Assert.NotNull (windowWithFrameRectEmpty);
        Assert.Equal ("title", windowWithFrameRectEmpty.Title);
        Assert.True (windowWithFrameRectEmpty.CanFocus);
        Assert.False (windowWithFrameRectEmpty.HasFocus);
        Assert.Null (windowWithFrameRectEmpty.Focused);
        Assert.NotNull (windowWithFrameRectEmpty.GetScheme ());
        Assert.Equal (0, windowWithFrameRectEmpty.X);
        Assert.Equal (0, windowWithFrameRectEmpty.Y);
        Assert.Equal (0, windowWithFrameRectEmpty.Width);
        Assert.Equal (0, windowWithFrameRectEmpty.Height);
        Assert.False (windowWithFrameRectEmpty.IsCurrentTop);
        Assert.False (windowWithFrameRectEmpty.MouseHoldRepeat);
        Assert.False (windowWithFrameRectEmpty.MousePositionTracking );
        Assert.Null (windowWithFrameRectEmpty.SuperView);
        Assert.Null (windowWithFrameRectEmpty.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, windowWithFrameRectEmpty.TextDirection);

        // Rect with values
        using var windowWithFrame1234 = new Window ();
        windowWithFrame1234.Frame = new (1, 2, 3, 4);
        windowWithFrame1234.Title = "title";
        Assert.Equal ("title", windowWithFrame1234.Title);
        Assert.NotNull (windowWithFrame1234);
        Assert.Equal ($"Window(){windowWithFrame1234.Frame}", windowWithFrame1234.ToString ());
        Assert.True (windowWithFrame1234.CanFocus);
        Assert.False (windowWithFrame1234.HasFocus);
        Assert.Equal (new (0, 0, 1, 2), windowWithFrame1234.Viewport);
        Assert.Equal (new (1, 2, 3, 4), windowWithFrame1234.Frame);
        Assert.Null (windowWithFrame1234.Focused);
        Assert.NotNull (windowWithFrame1234.GetScheme ());
        Assert.Equal (1, windowWithFrame1234.X);
        Assert.Equal (2, windowWithFrame1234.Y);
        Assert.Equal (3, windowWithFrame1234.Width);
        Assert.Equal (4, windowWithFrame1234.Height);
        Assert.False (windowWithFrame1234.IsCurrentTop);
        Assert.False (windowWithFrame1234.MouseHoldRepeat);
        Assert.False (windowWithFrame1234.MousePositionTracking );
        Assert.Null (windowWithFrame1234.SuperView);
        Assert.Null (windowWithFrame1234.MostFocused);
        Assert.Equal (TextDirection.LeftRight_TopBottom, windowWithFrame1234.TextDirection);
    }
}
