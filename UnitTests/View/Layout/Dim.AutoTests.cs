using System.Globalization;
using System.Text;
using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.PosDimTests;

public class DimAutoTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

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

        superView.SetRelativeLayout (new (10, 10));
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

        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);

        subView.X = -1;
        subView.Y = -1;
        superView.SetRelativeLayout (new (10, 10));
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

        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubviews (); // no throw

        Assert.Equal (10, superView.Frame.Width);
        Assert.Equal (10, superView.Frame.Height);

        subView.Width = 3;
        subView.Height = 3;
        superView.SetRelativeLayout (new (10, 10));
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
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, 10, expectedHeight), superView.Frame);
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
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, 0, 0), superView.Frame);

        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, 0, 0), superView.Frame);
    }

    [Fact]
    public void NoSubViews_Does_Nothing_Vertical ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (),
            Height = Dim.Auto (),
            TextDirection = TextDirection.TopBottom_LeftRight,
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, 0, 0), superView.Frame);

        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, 0, 0), superView.Frame);
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
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, expectedWidth, expectedHeight), superView.Frame);
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
        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubviews (); // no throw

        subView.Width = Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Width = 10;

        subView.Height = Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 10;

        subView.Height = Dim.Percent (50);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 10;

        subView.X = Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.X = 0;

        subView.Y = Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Y = 0;

        subView.Width = 10;
        subView.Height = 10;
        subView.X = 0;
        subView.Y = 0;
        superView.SetRelativeLayout (new (0, 0));
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
        superView.SetRelativeLayout (new (0, 0));
        superView.LayoutSubviews (); // no throw

        subView.Height = Dim.Fill () + 3;
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 0;

        subView.Height = 3 + Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 0;

        subView.Height = 3 + 5 + Dim.Fill ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 0;

        subView.Height = 3 + 5 + Dim.Percent (10);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 0;

        // Tests nested Combine
        subView.Height = 5 + new Dim.DimCombine (true, 3, new Dim.DimCombine (true, Dim.Percent (10), 9));
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
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
        superView.SetRelativeLayout (new (0, 0));
        superView.LayoutSubviews (); // no throw

        subView.X = Pos.Right (subView2);
        superView.SetRelativeLayout (new (0, 0));
        superView.LayoutSubviews (); // no throw

        subView.X = Pos.Right (subView2) + 3;
        superView.SetRelativeLayout (new (0, 0)); // no throw
        superView.LayoutSubviews (); // no throw

        subView.X = new Pos.PosCombine (true, Pos.Right (subView2), new Pos.PosCombine (true, 7, 9));
        superView.SetRelativeLayout (new (0, 0)); // no throw

        subView.X = Pos.Center () + 3;
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.X = 0;

        subView.X = 3 + Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.X = 0;

        subView.X = 3 + 5 + Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.X = 0;

        subView.X = 3 + 5 + Pos.Percent (10);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.X = 0;

        subView.X = Pos.Percent (10) + Pos.Center ();
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.X = 0;

        // Tests nested Combine
        subView.X = 5 + new Pos.PosCombine (true, Pos.Right (subView2), new Pos.PosCombine (true, Pos.Center (), 9));
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
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
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new Rectangle (0, 0, expectedWidth, 10), superView.Frame);
    }

    // Test that when a view has Width set to DimAuto (min: x) the width is never < x even if SetRelativeLayout is called with smaller bounds
    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (3, 3)]
    [InlineData (4, 4)]
    [InlineData (5, 4)] // This is clearly invalid, but we choose to not throw but log a debug message
    public void Width_Auto_Min (int min, int expectedWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (min: min),
            Height = 1,
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new (4, 1));
        Assert.Equal (expectedWidth, superView.Frame.Width);
    }

    // Test Dim.Fill - Fill should not impact width of the DimAuto superview
    [Theory]
    [InlineData (0, 0, 0, 10, 10)]
    [InlineData (0, 1, 0, 10, 10)]
    [InlineData (0, 11, 0, 10, 10)]
    [InlineData (0, 10, 0, 10, 10)]
    [InlineData (0, 5, 0, 10, 10)]
    [InlineData (1, 5, 0, 10, 9)]
    [InlineData (1, 10, 0, 10, 9)]
    [InlineData (0, 0, 1, 10, 9)]
    [InlineData (0, 10, 1, 10, 9)]
    [InlineData (0, 5, 1, 10, 9)]
    [InlineData (1, 5, 1, 10, 8)]
    [InlineData (1, 10, 1, 10, 8)]
    public void Width_Fill_Fills (int subX, int superMinWidth, int fill, int expectedSuperWidth, int expectedSubWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Dim.Auto (min: superMinWidth),
            Height = 1,
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = subX,
            Y = 0,
            Width = Dim.Fill (fill),
            Height = 1,
            ValidatePosDim = true
        };

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new (10, 1));
        Assert.Equal (expectedSuperWidth, superView.Frame.Width);
        superView.LayoutSubviews ();
        Assert.Equal (expectedSubWidth, subView.Frame.Width);
        Assert.Equal (expectedSuperWidth, superView.Frame.Width);
    }

    [Theory]
    [InlineData (0, 1, 1)]
    [InlineData (1, 1, 1)]
    [InlineData (9, 1, 1)]
    [InlineData (10, 1, 1)]
    [InlineData (0, 10, 10)]
    [InlineData (1, 10, 10)]
    [InlineData (9, 10, 10)]
    [InlineData (10, 10, 10)]
    public void Width_Auto_Text_Does_Not_Constrain_To_SuperView (int subX, int textLen, int expectedSubWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1,
            ValidatePosDim = true
        };

        var subView = new View
        {
            Text = new string ('*', textLen),
            X = subX,
            Y = 0,
            Width = Dim.Auto (Dim.DimAutoStyle.Text),
            Height = 1,
            ValidatePosDim = true
        };

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (superView.ContentSize);

        superView.LayoutSubviews ();
        Assert.Equal (expectedSubWidth, subView.Frame.Width);
    }

    [Theory]
    [InlineData (0, 1, 1)]
    [InlineData (1, 1, 1)]
    [InlineData (9, 1, 1)]
    [InlineData (10, 1, 1)]
    [InlineData (0, 10, 10)]
    [InlineData (1, 10, 10)]
    [InlineData (9, 10, 10)]
    [InlineData (10, 10, 10)]
    public void Width_Auto_Subviews_Does_Not_Constrain_To_SuperView (int subX, int subSubViewWidth, int expectedSubWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = 1,
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = subX,
            Y = 0,
            Width = Dim.Auto (Dim.DimAutoStyle.Subviews),
            Height = 1,
            ValidatePosDim = true
        };

        var subSubView = new View
        {
            X = 0,
            Y = 0,
            Width = subSubViewWidth,
            Height = 1,
            ValidatePosDim = true
        };
        subView.Add (subSubView);

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (superView.ContentSize);

        superView.LayoutSubviews ();
        Assert.Equal (expectedSubWidth, subView.Frame.Width);
    }

    [Fact]
    public void DimAuto_Text_Viewport_Stays_Set ()
    {
        var super = new View ()
        {
            Width = Dim.Fill (),
            Height = Dim.Fill ()
        };

        var view = new View ()
        {
            Text = "01234567",
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text),
        };

        super.Add (view);

        Rectangle expectedViewport = new (0, 0, 8, 1);
        Assert.Equal (expectedViewport.Size, view.ContentSize);
        Assert.Equal (expectedViewport, view.Frame);
        Assert.Equal (expectedViewport, view.Viewport);

        super.LayoutSubviews ();
        Assert.Equal (expectedViewport, view.Viewport);

        super.Dispose ();
    }


    // Test that changing TextFormatter does not impact View dimensions if Dim.Auto is not in play
    [Fact]
    public void DimAuto_Not_Used_TextFormatter_Does_Not_Change_View_Size ()
    {
        View view = new ()
        {
            Text = "_1234"
        };
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.Text = "ABC";
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.Alignment = TextAlignment.Justified;
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.VerticalAlignment = VerticalTextAlignment.Middle;
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.HotKeySpecifier = (Rune)'*';
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextFormatter.Text = "*ABC";
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);
    }


    [Fact]
    public void DimAuto_Not_Used_TextSettings_Do_Not_Change_View_Size ()
    {
        View view = new ()
        {
            Text = "_1234"
        };
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.TextAlignment = TextAlignment.Justified;
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.VerticalTextAlignment = VerticalTextAlignment.Middle;
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.HotKeySpecifier = (Rune)'*';
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);

        view.Text = "*ABC";
        Assert.False (view.TextFormatter.AutoSize);
        Assert.Equal (Size.Empty, view.Frame.Size);
    }


    [Fact]
    public void DimAuto_TextSettings_Change_View_Size ()
    {
        View view = new ()
        {
            Text = "_1234",
            Width = Dim.Auto ()
        };
        Assert.True (view.TextFormatter.AutoSize);
        Assert.NotEqual (Size.Empty, view.Frame.Size);

        view.TextAlignment = TextAlignment.Justified;
        Assert.True (view.TextFormatter.AutoSize);
        Assert.NotEqual (Size.Empty, view.Frame.Size);

        view = new ()
        {
            Text = "_1234",
            Width = Dim.Auto ()
        };
        view.VerticalTextAlignment = VerticalTextAlignment.Middle;
        Assert.True (view.TextFormatter.AutoSize);
        Assert.NotEqual (Size.Empty, view.Frame.Size);

        view = new ()
        {
            Text = "_1234",
            Width = Dim.Auto ()
        };
        view.HotKeySpecifier = (Rune)'*';
        Assert.True (view.TextFormatter.AutoSize);
        Assert.NotEqual (Size.Empty, view.Frame.Size);

        view = new ()
        {
            Text = "_1234",
            Width = Dim.Auto ()
        };
        view.Text = "*ABC";
        Assert.True (view.TextFormatter.AutoSize);
        Assert.NotEqual (Size.Empty, view.Frame.Size);
    }

    [Fact]
    public void  DimAuto_TextFormatter_Is_Auto ()
    {
        View view = new ();
        Assert.False (view.TextFormatter.AutoSize);
        view.Width = Dim.Auto ();
        Assert.True (view.TextFormatter.AutoSize);

        view = new ();
        Assert.False (view.TextFormatter.AutoSize);
        view.Height = Dim.Auto ();
        Assert.True (view.TextFormatter.AutoSize);
    }

    [Theory]
    [InlineData ("1234", 4)]
    [InlineData ("_1234", 4)]
    public void Width_Auto_HotKey_TextFormatter_Size_Correct (string text, int expected)
    {
        View view = new ()
        {
            Text = text,
            Height = 1,
            Width = Dim.Auto ()
        };
        Assert.Equal (new (expected, 1), view.TextFormatter.Size);
    }

    [Theory]
    [InlineData ("1234", 4)]
    [InlineData ("_1234", 4)]
    public void Height_Auto_HotKey_TextFormatter_Size_Correct (string text, int expected)
    {
        View view = new ()
        {
            HotKeySpecifier = (Rune)'_',
            Text = text,
            Width = Auto (),
            Height = 1,
        };
        Assert.Equal (new (expected, 1), view.TextFormatter.Size);

        view = new ()
        {
            HotKeySpecifier = (Rune)'_',
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = text,
            Width = 1,
            Height = Auto (),
        };
        Assert.Equal (new (1, expected), view.TextFormatter.Size);
    }


    [SetupFakeDriver]
    [Fact]
    public void DimAuto_ChangeToANonDimAuto_Resets_ContentSize ()
    {
        View view = new ()
        {
            Width = Auto (),
            Height = Auto (),
            Text = "01234"
        };

        Assert.Equal (new Rectangle (0, 0, 5, 1), view.Frame);
        Assert.Equal (new Size (5, 1), view.ContentSize);

        // Change text to a longer string
        view.Text = "0123456789";

        Assert.Equal (new Rectangle (0, 0, 10, 1), view.Frame);
        Assert.Equal (new Size (10, 1), view.ContentSize);

        // If ContentSize was reset, these should cause it to update
        view.Width = 5;
        view.Height = 1;

        Assert.Equal (new Size (5, 1), view.ContentSize);
    }
    [SetupFakeDriver]
    [Fact]
    public void DimAuto_ChangeNonDimAuto_Via_AutoSize_False_Resets_ContentSize ()
    {
        View view = new ()
        {
            Width = Auto (),
            Height = Auto(),
            Text = "01234"
        };

        Assert.Equal (new Rectangle (0, 0, 5, 1), view.Frame);
        Assert.Equal (new Size (5, 1), view.ContentSize);

        // Change text to a longer string
        view.Text = "0123456789";

        Assert.Equal (new Rectangle (0, 0, 10, 1), view.Frame);
        Assert.Equal (new Size (10, 1), view.ContentSize);

        // Cause Width/Height to be set to absolute. This should reset ContentSize
        view.AutoSize = false;

        Assert.Equal (new Rectangle (0, 0, 10, 1), view.Frame);
        Assert.Equal (new Size (10, 1), view.ContentSize);

        // If ContentSize was reset, these should cause it to update
        view.Width = 5;
        view.Height = 1;

        Assert.Equal(new Size (5,1), view.ContentSize);
    }


    // Test variations of Frame
}
