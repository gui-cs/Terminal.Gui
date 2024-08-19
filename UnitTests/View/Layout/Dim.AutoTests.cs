﻿using System.Text;
using Xunit.Abstractions;
using static Terminal.Gui.Dim;

namespace Terminal.Gui.LayoutTests;

[Trait("Category", "Layout")]
public partial class DimAutoTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

    [SetupFakeDriver]
    [Fact]
    public void Change_To_Non_Auto_Resets_ContentSize ()
    {
        View view = new ()
        {
            Width = Auto (),
            Height = Auto (),
            Text = "01234"
        };
        view.SetRelativeLayout (new (100, 100));
        Assert.Equal (new (0, 0, 5, 1), view.Frame);
        Assert.Equal (new (5, 1), view.GetContentSize ());

        // Change text to a longer string
        view.Text = "0123456789";

        Assert.Equal (new (0, 0, 10, 1), view.Frame);
        Assert.Equal (new (10, 1), view.GetContentSize ());

        // If ContentSize was reset, these should cause it to update
        view.Width = 5;
        view.Height = 1;

        Assert.Equal (new (5, 1), view.GetContentSize ());
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
    public void Height_Auto_Width_Absolute_NotChanged (int subX, int subY, int subWidth, int subHeight, int expectedHeight)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = 10,
            Height = Auto (),
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
        Assert.Equal (new (0, 0, 10, expectedHeight), superView.Frame);
    }

    [Theory]
    [CombinatorialData]
    public void HotKey_TextFormatter_Height_Correct ([CombinatorialValues ("1234", "_1234", "1_234", "____")] string text)
    {
        View view = new ()
        {
            HotKeySpecifier = (Rune)'_',
            Text = text,
            Width = Auto (),
            Height = 1
        };
        Assert.Equal (4, view.TextFormatter.ConstrainToWidth);
        Assert.Equal (1, view.TextFormatter.ConstrainToHeight);

        view = new ()
        {
            HotKeySpecifier = (Rune)'_',
            TextDirection = TextDirection.TopBottom_LeftRight,
            Text = text,
            Width = 1,
            Height = Auto ()
        };
        Assert.Equal (1, view.TextFormatter.ConstrainToWidth);
        Assert.Equal (4, view.TextFormatter.ConstrainToHeight);
    }

    [Theory]
    [CombinatorialData]
    public void HotKey_TextFormatter_Width_Correct ([CombinatorialValues ("1234", "_1234", "1_234", "____")] string text)
    {
        View view = new ()
        {
            Text = text,
            Height = 1,
            Width = Auto ()
        };
        Assert.Equal (4, view.TextFormatter.ConstrainToWidth);
        Assert.Equal (1, view.TextFormatter.ConstrainToHeight);
    }

    [Fact]
    public void NoSubViews_Does_Nothing ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (),
            Height = Auto (),
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (0, 0, 0, 0), superView.Frame);

        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (0, 0, 0, 0), superView.Frame);
    }

    [Fact]
    public void NoSubViews_Does_Nothing_Vertical ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (),
            Height = Auto (),
            TextDirection = TextDirection.TopBottom_LeftRight,
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (0, 0, 0, 0), superView.Frame);

        superView.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (0, 0, 0, 0), superView.Frame);
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
    public void SubView_Changes_SuperView_Size (int subX, int subY, int subWidth, int subHeight, int expectedWidth, int expectedHeight)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (),
            Height = Auto (),
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
        Assert.Equal (new (0, 0, expectedWidth, expectedHeight), superView.Frame);
    }

    [Fact]
    public void TestEquality ()
    {
        var a = new DimAuto (
                             MaximumContentDim: null,
                             MinimumContentDim: 1,
                             Style: DimAutoStyle.Auto
                            );

        var b = new DimAuto (
                             MaximumContentDim: null,
                             MinimumContentDim: 1,
                             Style: DimAutoStyle.Auto
                            );

        var c = new DimAuto(
                             MaximumContentDim: 2,
                             MinimumContentDim: 1,
                             Style: DimAutoStyle.Auto
                            );

        var d = new DimAuto (
                             MaximumContentDim: null,
                             MinimumContentDim: 1,
                             Style: DimAutoStyle.Content
                            );

        var e = new DimAuto (
                             MaximumContentDim: null,
                             MinimumContentDim: 2,
                             Style: DimAutoStyle.Auto
                            );

        // Test equality with same values
        Assert.True (a.Equals (b));
        Assert.True (a.GetHashCode () == b.GetHashCode ());

        // Test inequality with different MaximumContentDim
        Assert.False (a.Equals (c));
        Assert.False (a.GetHashCode () == c.GetHashCode ());

        // Test inequality with different Style
        Assert.False (a.Equals (d));
        Assert.False (a.GetHashCode () == d.GetHashCode ());

        // Test inequality with different MinimumContentDim
        Assert.False (a.Equals (e));
        Assert.False (a.GetHashCode () == e.GetHashCode ());

        // Test inequality with null
        Assert.False (a.Equals (null));
    }

    [Fact]
    public void TestEquality_Simple ()
    {
        Dim a = Auto ();
        Dim b = Auto ();
        Assert.True (a.Equals (b));
        Assert.True (a.GetHashCode () == b.GetHashCode ());
    }

    [Fact]
    public void TextFormatter_Settings_Change_View_Size ()
    {
        View view = new ()
        {
            Text = "_1234",
            Width = Auto ()
        };
        Assert.Equal (new (4, 0), view.Frame.Size);

        view.Height = 1;
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.Equal (new (4, 1), view.Frame.Size);
        Size lastSize = view.Frame.Size;

        view.TextAlignment = Alignment.Fill;
        Assert.Equal (lastSize, view.Frame.Size);

        view = new ()
        {
            Text = "_1234",
            Width = Auto (),
            Height = 1
        };
        view.SetRelativeLayout (Application.Screen.Size);

        lastSize = view.Frame.Size;
        view.VerticalTextAlignment = Alignment.Center;
        Assert.Equal (lastSize, view.Frame.Size);

        view = new ()
        {
            Text = "_1234",
            Width = Auto (),
            Height = 1
        };
        view.SetRelativeLayout (Application.Screen.Size);
        lastSize = view.Frame.Size;
        view.HotKeySpecifier = (Rune)'*';
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.NotEqual (lastSize, view.Frame.Size);

        view = new ()
        {
            Text = "_1234",
            Width = Auto (),
            Height = 1
        };
        view.SetRelativeLayout (Application.Screen.Size);
        lastSize = view.Frame.Size;
        view.Text = "*ABCD";
        Assert.NotEqual (lastSize, view.Frame.Size);
    }

    // Test validation
    [Fact]
    public void ValidatePosDim_True_Throws_When_SubView_Uses_SuperView_Dims ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (),
            Height = Auto (),
            ValidatePosDim = true
        };

        var subView = new View
        {
            X = 0,
            Y = 0,
            Width = Fill (),
            Height = 10,
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.Add (subView);

        subView.Width = 10;
        superView.Add (subView);
        superView.SetRelativeLayout (new (10, 10));
        superView.LayoutSubviews (); // no throw

        subView.Width = Fill ();
        superView.SetRelativeLayout (new (0, 0));
        subView.Width = 10;

        subView.Height = Fill ();
        superView.SetRelativeLayout (new (0, 0));
        subView.Height = 10;

        subView.Height = Percent (50);
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
            Width = Auto (),
            Height = Auto (),
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

        subView.Height = Fill () + 3;
        superView.SetRelativeLayout (new (0, 0));
        subView.Height = 0;

        subView.Height = 3 + Fill ();
        superView.SetRelativeLayout (new (0, 0));
        subView.Height = 0;

        subView.Height = 3 + 5 + Fill ();
        superView.SetRelativeLayout (new (0, 0));
        subView.Height = 0;

        subView.Height = 3 + 5 + Percent (10);
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
        subView.Height = 0;

        // Tests nested Combine
        subView.Height = 5 + new DimCombine (AddOrSubtract.Add, 3, new DimCombine (AddOrSubtract.Add, Percent (10), 9));
        Assert.Throws<InvalidOperationException> (() => superView.SetRelativeLayout (new (0, 0)));
    }

    [Fact]
    public void ValidatePosDim_True_Throws_When_SubView_Uses_SuperView_Pos_Combine ()
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (),
            Height = Auto (),
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

        subView.X = new PosCombine (AddOrSubtract.Add, Pos.Right (subView2), new PosCombine (AddOrSubtract.Add, 7, 9));
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
        subView.X = 5 + new PosCombine (AddOrSubtract.Add, Pos.Right (subView2), new PosCombine (AddOrSubtract.Add, Pos.Center (), 9));
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
    public void Width_Auto_Height_Absolute_NotChanged (int subX, int subY, int subWidth, int subHeight, int expectedWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (),
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
        Assert.Equal (new (0, 0, expectedWidth, 10), superView.Frame);
    }

    // Test that when a view has Width set to DimAuto (min: x)
    // the width is never < x even if SetRelativeLayout is called with smaller bounds
    [Theory]
    [InlineData (0, 0)]
    [InlineData (1, 1)]
    [InlineData (3, 3)]
    [InlineData (4, 4)]
    [InlineData (5, 5)] // No reason why it can exceed container
    public void Width_Auto_Min_Honored (int min, int expectedWidth)
    {
        var superView = new View
        {
            X = 0,
            Y = 0,
            Width = Auto (minimumContentDim: min),
            Height = 1,
            ValidatePosDim = true
        };

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (new (4, 1));
        Assert.Equal (expectedWidth, superView.Frame.Width);
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
            Width = Auto (DimAutoStyle.Content),
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
        superView.SetRelativeLayout (superView.GetContentSize ());

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
            Text = new ('*', textLen),
            X = subX,
            Y = 0,
            Width = Auto (DimAutoStyle.Text),
            Height = 1,
            ValidatePosDim = true
        };

        superView.Add (subView);

        superView.BeginInit ();
        superView.EndInit ();
        superView.SetRelativeLayout (superView.GetContentSize ());

        superView.LayoutSubviews ();
        Assert.Equal (expectedSubWidth, subView.Frame.Width);
    }

    private class DimAutoTestView : View
    {
        public DimAutoTestView ()
        {
            ValidatePosDim = true;
            Width = Auto ();
            Height = Auto ();
        }

        public DimAutoTestView (Dim width, Dim height)
        {
            ValidatePosDim = true;
            Width = width;
            Height = height;
        }

        public DimAutoTestView (string text, Dim width, Dim height)
        {
            ValidatePosDim = true;
            Text = text;
            Width = width;
            Height = height;
        }
    }

    #region DimAutoStyle.Auto tests

    [Theory]
    [InlineData ("", 0, 0)]
    [InlineData (" ", 1, 1)]
    [InlineData ("01234", 5, 1)]
    [InlineData ("01234\nABCDE", 5, 2)]
    public void DimAutoStyle_Auto_JustText_Sizes_Correctly (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Width = Auto ();
        view.Height = Auto ();

        view.Text = text;

        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (expectedW, expectedH), view.Frame.Size);
    }

    [Fact]
    public void DimAutoStyle_Auto_Text_Size_Is_Used ()
    {
        var view = new View
        {
            Text = "0123\n4567",
            Width = Auto (),
            Height = Auto ()
        };

        view.SetRelativeLayout (new (100, 100));
        Assert.Equal (new (4, 2), view.Frame.Size);

        var subView = new View
        {
            Text = "ABCD",
            Width = Auto (),
            Height = Auto ()
        };
        view.Add (subView);

        view.SetRelativeLayout (new (100, 100));
        Assert.Equal (new (4, 2), view.Frame.Size);

        subView.Text = "ABCDE";

        view.SetRelativeLayout (new (100, 100));
        Assert.Equal (new (5, 2), view.Frame.Size);
    }

    [Theory]
    [InlineData ("01234", 5, 5)]
    [InlineData ("01234", 6, 6)]
    [InlineData ("01234", 4, 5)]
    [InlineData ("01234", 0, 5)]
    [InlineData ("", 5, 5)]
    [InlineData ("", 0, 0)]
    public void DimAutoStyle_Auto_Larger_Wins (string text, int dimension, int expected)
    {
        View view = new ()
        {
            Width = Auto (),
            Height = 1,
            Text = text
        };

        View subView = new ()
        {
            Width = dimension,
            Height = 1
        };
        view.Add (subView);

        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (expected, view.Frame.Width);
    }

    #endregion

    #region DimAutoStyle.Text tests

    [Fact]
    public void DimAutoStyle_Text_Viewport_Stays_Set ()
    {
        var super = new View
        {
            Width = Fill (),
            Height = Fill ()
        };

        var view = new View
        {
            Text = "01234567",
            Width = Auto (DimAutoStyle.Text),
            Height = Auto (DimAutoStyle.Text)
        };

        super.Add (view);

        Rectangle expectedViewport = new (0, 0, 8, 1);
        Assert.Equal (expectedViewport.Size, view.GetContentSize ());
        Assert.Equal (expectedViewport, view.Frame);
        Assert.Equal (expectedViewport, view.Viewport);

        super.LayoutSubviews ();
        Assert.Equal (expectedViewport, view.Viewport);

        super.Dispose ();
    }

    [Theory]
    [InlineData ("", 0, 0)]
    [InlineData (" ", 1, 1)]
    [InlineData ("01234", 5, 1)]
    [InlineData ("01234\nABCDE", 5, 2)]
    public void DimAutoStyle_Text_Sizes_Correctly (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Width = Auto (DimAutoStyle.Text);
        view.Height = Auto (DimAutoStyle.Text);

        view.Text = text;

        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (expectedW, expectedH), view.Frame.Size);
    }

    [Theory]
    [InlineData ("", 0, 0, 0, 0)]
    [InlineData (" ", 5, 5, 5, 5)]
    [InlineData ("01234", 5, 5, 5, 5)]
    [InlineData ("01234", 4, 3, 5, 3)]
    [InlineData ("01234ABCDE", 5, 0, 10, 1)]
    public void DimAutoStyle_Text_Sizes_Correctly_With_Min (string text, int minWidth, int minHeight, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Width = Auto (DimAutoStyle.Text, minWidth);
        view.Height = Auto (DimAutoStyle.Text, minHeight);

        view.Text = text;

        view.SetRelativeLayout (Application.Screen.Size);

        Assert.Equal (new (expectedW, expectedH), view.Frame.Size);
    }

    [Theory]
    [InlineData ("", 0, 0, 0)]
    [InlineData (" ", 5, 1, 1)]
    [InlineData ("01234", 5, 5, 1)]
    [InlineData ("01234", 4, 4, 2)]
    [InlineData ("01234ABCDE", 5, 5, 2)]
    [InlineData ("01234ABCDE", 1, 1, 10)]
    public void DimAutoStyle_Text_Sizes_Correctly_With_Max_Width (string text, int maxWidth, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Width = Auto (DimAutoStyle.Text, maximumContentDim: maxWidth);
        view.Height = Auto (DimAutoStyle.Text);
        view.Text = text;

        Assert.Equal (new (expectedW, expectedH), view.Frame.Size);
    }

    [Theory]
    [InlineData ("", 0, 0)]
    [InlineData (" ", 1, 1)]
    [InlineData ("01234", 5, 1)]
    [InlineData ("01234ABCDE", 10, 1)]
    [InlineData ("01234\nABCDE", 5, 2)]
    public void DimAutoStyle_Text_NoMin_Not_Constrained_By_ContentSize (string text, int expectedW, int expectedH)
    {
        var view = new View ();
        view.Width = Auto (DimAutoStyle.Text);
        view.Height = Auto (DimAutoStyle.Text);
        view.SetContentSize (new (1, 1));
        view.Text = text;
        view.SetRelativeLayout (Application.Screen.Size);
        Assert.Equal (new (expectedW, expectedH), view.Frame.Size);
    }

    [Theory]
    [InlineData ("", 0, 0)]
    [InlineData (" ", 1, 1)]
    [InlineData ("01234", 5, 1)]
    [InlineData ("01234ABCDE", 10, 1)]
    [InlineData ("01234\nABCDE", 5, 2)]
    public void DimAutoStyle_Text_NoMin_Not_Constrained_By_SuperView (string text, int expectedW, int expectedH)
    {
        var superView = new View
        {
            Width = 1, Height = 1
        };

        var view = new View ();

        view.Width = Auto (DimAutoStyle.Text);
        view.Height = Auto (DimAutoStyle.Text);
        view.Text = text;
        superView.Add (view);

        superView.SetRelativeLayout (Application.Screen.Size);
        Assert.Equal (new (expectedW, expectedH), view.Frame.Size);
    }

    [Fact]
    public void DimAutoStyle_Text_Pos_AnchorEnd_Locates_Correctly ()
    {
        DimAutoTestView view = new ("01234", Auto (DimAutoStyle.Text), Auto (DimAutoStyle.Text));

        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 0), view.Frame.Location);

        view.X = 0;

        view.Y = Pos.AnchorEnd (1);
        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 9), view.Frame.Location);

        view.Y = Pos.AnchorEnd ();
        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 9), view.Frame.Location);

        view.Y = Pos.AnchorEnd () - 1;
        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 8), view.Frame.Location);
    }

    #endregion DimAutoStyle.Text tests

    #region DimAutoStyle.Content tests

    // DimAutoStyle.Content tests
    [Fact]
    public void DimAutoStyle_Content_UsesContentSize_WhenSet ()
    {
        var view = new View ();
        view.SetContentSize (new (10, 5));

        Dim dim = Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);

        Assert.Equal (10, calculatedWidth);
    }

    [Fact]
    public void DimAutoStyle_Content_IgnoresSubviews_When_ContentSize_Is_Set ()
    {
        var view = new View ();

        var subview = new View
        {
            Frame = new (50, 50, 1, 1)
        };
        view.SetContentSize (new (10, 5));

        Dim dim = Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);

        Assert.Equal (10, calculatedWidth);
    }

    [Fact]
    public void DimAutoStyle_Content_IgnoresText_WhenContentSizeNotSet ()
    {
        var view = new View { Text = "This is a test" };
        Dim dim = Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);

        Assert.Equal (0, calculatedWidth); // Assuming 0 is the default when no ContentSize or Subviews are set
    }

    [Fact]
    public void DimAutoStyle_Content_UsesLargestSubview_WhenContentSizeNotSet ()
    {
        var view = new View ();
        view.Add (new View { Frame = new (0, 0, 5, 5) }); // Smaller subview
        view.Add (new View { Frame = new (0, 0, 10, 10) }); // Larger subview

        Dim dim = Auto (DimAutoStyle.Content);

        int calculatedWidth = dim.Calculate (0, 100, view, Dimension.Width);

        Assert.Equal (10, calculatedWidth); // Expecting the size of the largest subview
    }

    [Fact]
    public void DimAutoStyle_Content_UsesContentSize_If_No_Subviews ()
    {
        DimAutoTestView view = new (Auto (DimAutoStyle.Content), Auto (DimAutoStyle.Content));
        view.SetContentSize (new (5, 5));
        view.SetRelativeLayout (new (10, 10));

        Assert.Equal (new (5, 5), view.Frame.Size);
    }

    [Fact]
    public void DimAutoStyle_Content_Pos_AnchorEnd_Locates_Correctly ()
    {
        DimAutoTestView view = new (Auto (DimAutoStyle.Content), Auto (DimAutoStyle.Content));

        View subView = new ()
        {
            Width = 5,
            Height = 1
        };
        view.Add (subView);

        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 0), view.Frame.Location);

        view.X = 0;

        view.Y = Pos.AnchorEnd (1);
        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 9), view.Frame.Location);

        view.Y = Pos.AnchorEnd ();
        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 9), view.Frame.Location);

        view.Y = Pos.AnchorEnd () - 1;
        view.SetRelativeLayout (new (10, 10));
        Assert.Equal (new (5, 1), view.Frame.Size);
        Assert.Equal (new (0, 8), view.Frame.Location);
    }

    #endregion DimAutoStyle.Content tests

    // Test variations of Frame
}
