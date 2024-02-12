using System.Globalization;
using System.Text;
using Xunit.Abstractions;

// Alias Console to MockConsole so we don't accidentally use Console
using Console = Terminal.Gui.FakeConsole;

namespace Terminal.Gui.ViewTests;

public class DimAutoTests
{
    private readonly ITestOutputHelper _output;

    public DimAutoTests (ITestOutputHelper output)
    {
        _output = output;
        Console.OutputEncoding = Encoding.Default;

        // Change current culture
        var culture = CultureInfo.CreateSpecificCulture ("en-US");
        Thread.CurrentThread.CurrentCulture = culture;
        Thread.CurrentThread.CurrentUICulture = culture;
    }

    // Test min - ensure that if min is specified in the DimAuto constructor it is honored
    [Fact]
    public void DimAuto_Min ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (min: 10),
            Height = Dim.Auto (min: 10),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);
    }

    // what happens if DimAuto (min: 10) and the subview moves to a negative coord?
    [Fact]
    public void DimAuto_Min_Resets_If_Subview_Moves_Negative ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (min: 10),
            Height = Dim.Auto (min: 10),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);

        subView.X = -1;
        subView.Y = -1;
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (5, subView.Frame.Width);
        Assert.Equal (5, subView.Frame.Height);

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);
    }

    [Fact]
    public void DimAuto_Min_Resets_If_Subview_Shrinks ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (min: 10),
            Height = Dim.Auto (min: 10),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 5,
            Height = 5
        };

        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();

        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);

        subView.Width = 3;
        subView.Height = 3;
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (3, subView.Frame.Width);
        Assert.Equal (3, subView.Frame.Height);

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0)]
    [InlineData (0, 0, 5, 0, 0)]
    [InlineData (0, 0, 0, 5, 5)]
    [InlineData (0, 0, 5, 5, 5)]
    [InlineData (1, 0, 5, 0, 0)]
    [InlineData (1, 0, 0, 5, 5)]
    [InlineData (1, 0, 5, 5, 5)]
    [InlineData (1, 1, 5, 5, 6)]
    [InlineData (-1, 0, 5, 0, 0)]
    [InlineData (-1, 0, 0, 5, 5)]
    [InlineData (-1, 0, 5, 5, 5)]
    [InlineData (-1, -1, 5, 5, 4)]
    public void Height_Auto_Width_NotChanged (int subX, int subY, int subWidth, int subHeight, int expectedHeight)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = Dim.Auto (),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = subX,
            Y = subY,
            Width = subWidth,
            Height = subHeight,
            ValidatePosDim = true
        };

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        Assert.Equal (new Rect (0, 0, 10, expectedHeight), superView.Frame);
    }

    [Fact]
    public void NoSubViews_Does_Nothing ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        Assert.Equal (new Rect (0, 0, 0, 0), superView.Frame);

        superView.SetRelativeLayout (new Rect (0, 0, 10, 10));
        Assert.Equal (new Rect (0, 0, 0, 0), superView.Frame);

        superView.SetRelativeLayout (new Rect (10, 10, 10, 10));
        Assert.Equal (new Rect (0, 0, 0, 0), superView.Frame);
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0, 0)]
    [InlineData (0, 0, 5, 0, 5, 0)]
    [InlineData (0, 0, 0, 5, 0, 5)]
    [InlineData (0, 0, 5, 5, 5, 5)]
    [InlineData (1, 0, 5, 0, 6, 0)]
    [InlineData (1, 0, 0, 5, 1, 5)]
    [InlineData (1, 0, 5, 5, 6, 5)]
    [InlineData (1, 1, 5, 5, 6, 6)]
    [InlineData (-1, 0, 5, 0, 4, 0)]
    [InlineData (-1, 0, 0, 5, 0, 5)]
    [InlineData (-1, 0, 5, 5, 4, 5)]
    [InlineData (-1, -1, 5, 5, 4, 4)]
    public void SubView_ChangesSuperViewSize (int subX, int subY, int subWidth, int subHeight, int expectedWidth, int expectedHeight)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = subX,
            Y = subY,
            Width = subWidth,
            Height = subHeight,
            ValidatePosDim = true
        };

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        Assert.Equal (new Rect (0, 0, expectedWidth, expectedHeight), superView.Frame);
    }

    // Test validation
    [Fact]
    public void ValidatePosDim_True_Throws_When_SubView_Uses_SuperView_Dims ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill (),
            Height = 10,
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();

        Assert.Throws<InvalidOperationException> (() => superView.Add (subView));

        subView.Width = 10;
        superView.Add (subView);
        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        subView.Width = Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Width = 10;

        subView.Height = Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Height = 10;

        subView.Height = Dim.Percent (50);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Height = 10;

        subView.X = Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;

        subView.Y = Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Y = 0;

        subView.Width = 10;
        subView.Height = 10;
        subView.X = 0;
        subView.Y = 0;
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews ();
    }

    // Test validation
    [Fact]
    public void ValidatePosDim_True_Throws_When_SubView_Uses_SuperView_Dims_Combine ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        var subView2 = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        superView.Add (subView, subView2);
        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        subView.Height = Dim.Fill () + 3;
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Height = 0;

        subView.Height = 3 + Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Height = 0;

        subView.Height = 3 + 5 + Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Height = 0;

        subView.Height = 3 + 5 + Dim.Percent (10);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.Height = 0;

        // Tests nested Combine
        subView.Height = 5 + new Dim.DimCombine (true, 3, new Dim.DimCombine (true, Dim.Percent (10), 9));
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
    }

    [Fact]
    public void ValidatePosDim_True_Throws_When_SubView_Uses_SuperView_Pos_Combine ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        var subView2 = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 10
        };

        superView.Add (subView, subView2);
        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        subView.X = Pos.Right (subView2);
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        superView.LayoutSubviews (); // no throw

        subView.X = Pos.Right (subView2) + 3;
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0)); // no throw
        superView.LayoutSubviews (); // no throw

        subView.X = new Pos.PosCombine (true, Pos.Right (subView2), new Pos.PosCombine (true, 7, 9));
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0)); // no throw

        subView.X = Pos.Center () + 3;
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;

        subView.X = 3 + Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;

        subView.X = 3 + 5 + Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;

        subView.X = 3 + 5 + Pos.Percent (10);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;

        subView.X = Pos.Percent (10) + Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;

        // Tests nested Combine
        subView.X = 5 + new Pos.PosCombine (true, Pos.Right (subView2), new Pos.PosCombine (true, Pos.Center (), 9));
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new Rect (0, 0, 0, 0)));
        subView.X = 0;
    }

    [Theory]
    [InlineData (0, 0, 0, 0, 0)]
    [InlineData (0, 0, 5, 0, 5)]
    [InlineData (0, 0, 0, 5, 0)]
    [InlineData (0, 0, 5, 5, 5)]
    [InlineData (1, 0, 5, 0, 6)]
    [InlineData (1, 0, 0, 5, 1)]
    [InlineData (1, 0, 5, 5, 6)]
    [InlineData (1, 1, 5, 5, 6)]
    [InlineData (-1, 0, 5, 0, 4)]
    [InlineData (-1, 0, 0, 5, 0)]
    [InlineData (-1, 0, 5, 5, 4)]
    [InlineData (-1, -1, 5, 5, 4)]
    public void Width_Auto_Height_NotChanged (int subX, int subY, int subWidth, int subHeight, int expectedWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = 10,
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = subX,
            Y = subY,
            Width = subWidth,
            Height = subHeight,
            ValidatePosDim = true
        };

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new Rect (0, 0, 0, 0));
        Assert.Equal (new Rect (0, 0, expectedWidth, 10), superView.Frame);
    }

    // Test variations of Frame
}
