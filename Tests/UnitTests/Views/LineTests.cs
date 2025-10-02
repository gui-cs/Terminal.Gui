using UnitTests;
using Xunit.Abstractions;

namespace Terminal.Gui.ViewsTests;

public class LineTests (ITestOutputHelper output)
{
    [Fact]
    [AutoInitShutdown]
    public void Line_DefaultConstructor_Horizontal ()
    {
        var line = new Line ();

        Assert.Equal (Orientation.Horizontal, line.Orientation);
        Assert.Equal (Dim.Fill (), line.Width);
        Assert.Equal (LineStyle.Single, line.Style);
        Assert.True (line.SuperViewRendersLineCanvas);
        Assert.False (line.CanFocus);

        line.Layout ();
        Assert.Equal (1, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_Horizontal_FillsWidth ()
    {
        var line = new Line { Orientation = Orientation.Horizontal };
        var container = new View { Width = 50, Height = 10 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (50, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_Vertical_FillsHeight ()
    {
        var line = new Line { Orientation = Orientation.Vertical };
        var container = new View { Width = 50, Height = 10 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (10, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_ChangeOrientation_UpdatesDimensions ()
    {
        var line = new Line { Orientation = Orientation.Horizontal };
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);
        container.Layout ();

        Assert.Equal (50, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);

        // Change to vertical
        line.Orientation = Orientation.Vertical;
        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (20, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_Style_CanBeSet ()
    {
        var line = new Line { Style = LineStyle.Double };

        Assert.Equal (LineStyle.Double, line.Style);
    }

    [Theory]
    [InlineData (LineStyle.Single)]
    [InlineData (LineStyle.Double)]
    [InlineData (LineStyle.Heavy)]
    [InlineData (LineStyle.Rounded)]
    [InlineData (LineStyle.Dashed)]
    [InlineData (LineStyle.Dotted)]
    [AutoInitShutdown]
    public void Line_SupportsDifferentLineStyles (LineStyle style)
    {
        var line = new Line { Style = style };

        Assert.Equal (style, line.Style);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_DrawsCalled_Successfully ()
    {
        var app = new Window ();
        var line = new Line { Y = 1, Width = 10 };
        app.Add (line);

        app.BeginInit ();
        app.EndInit ();
        app.Layout ();
        
        // Just verify the line can be drawn without errors
        var exception = Record.Exception(() => app.Draw ());
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_WithBorder_DrawsSuccessfully ()
    {
        var app = new Window { Width = 20, Height = 10, BorderStyle = LineStyle.Single };

        // Add a line that intersects with the window border
        var line = new Line { X = 5, Y = 0, Height = Dim.Fill (), Orientation = Orientation.Vertical };
        app.Add (line);

        app.BeginInit ();
        app.EndInit ();
        app.Layout ();
        
        // Just verify the line and border can be drawn together without errors
        var exception = Record.Exception(() => app.Draw ());
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_MultipleIntersecting_DrawsSuccessfully ()
    {
        var app = new Window { Width = 30, Height = 15 };

        // Create intersecting lines
        var hLine = new Line { X = 5, Y = 5, Width = 15, Style = LineStyle.Single };
        var vLine = new Line
        {
            X = 12, Y = 2, Height = 8, Orientation = Orientation.Vertical, Style = LineStyle.Single
        };

        app.Add (hLine, vLine);

        app.BeginInit ();
        app.EndInit ();
        app.Layout ();
        
        // Just verify multiple intersecting lines can be drawn without errors
        var exception = Record.Exception(() => app.Draw ());
        Assert.Null (exception);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_ExplicitWidthAndHeight_RespectValues ()
    {
        var line = new Line { Width = 10, Height = 1 };
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (10, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_VerticalWithExplicitHeight_RespectValues ()
    {
        var line = new Line { Orientation = Orientation.Vertical };
        // Set height AFTER orientation to avoid it being reset
        line.Width = 1;
        line.Height = 8;
        
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);

        container.Layout ();

        Assert.Equal (1, line.Frame.Width);
        Assert.Equal (8, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_SuperViewRendersLineCanvas_IsTrue ()
    {
        var line = new Line ();

        Assert.True (line.SuperViewRendersLineCanvas);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_CannotFocus ()
    {
        var line = new Line ();

        Assert.False (line.CanFocus);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_ImplementsIOrientation ()
    {
        var line = new Line ();

        Assert.IsAssignableFrom<IOrientation> (line);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_SetHeight_PreservesOnOrientationChange ()
    {
        var line = new Line ();
        
        // Set height before changing orientation
        line.SetHeight(5);
        
        // Change orientation - height should be preserved
        line.Orientation = Orientation.Vertical;
        
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);
        container.Layout ();
        
        Assert.Equal (5, line.Frame.Height);
        Assert.Equal (1, line.Frame.Width); // Width should still be set to 1 for vertical
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_SetWidth_PreservesOnOrientationChange ()
    {
        var line = new Line ();
        
        // Set width before changing orientation
        line.SetWidth(10);
        
        // Change orientation - width should be preserved
        line.Orientation = Orientation.Horizontal;
        
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);
        container.Layout ();
        
        Assert.Equal (10, line.Frame.Width);
        Assert.Equal (1, line.Frame.Height); // Height should still be set to 1 for horizontal
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_SetWidthAndHeight_BothPreservedOnOrientationChange ()
    {
        var line = new Line ();
        
        // Set both width and height
        line.SetWidth(15);
        line.SetHeight(8);
        
        // Change orientation - both should be preserved
        line.Orientation = Orientation.Vertical;
        
        var container = new View { Width = 50, Height = 20 };
        container.Add (line);
        container.Layout ();
        
        Assert.Equal (15, line.Frame.Width);
        Assert.Equal (8, line.Frame.Height);
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_Draw_DoesNotThrow ()
    {
        var top = new Toplevel ();
        var win = new Window { Width = 10, Height = 5, BorderStyle = LineStyle.None };
        top.Add (win);

        var line = new Line { X = 1, Y = 1, Width = 5, Style = LineStyle.Single };
        win.Add (line);

        RunState rs = Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize (new Size (10, 5));
        
        var exception = Record.Exception(() => top.Draw ());
        Assert.Null (exception);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_Vertical_Draw_DoesNotThrow ()
    {
        var top = new Toplevel ();
        var win = new Window { Width = 10, Height = 7, BorderStyle = LineStyle.None };
        top.Add (win);

        var line = new Line
        {
            X = 2, Y = 1, Height = 4, Orientation = Orientation.Vertical, Style = LineStyle.Single
        };
        win.Add (line);

        RunState rs = Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize (new Size (10, 7));
        
        var exception = Record.Exception(() => top.Draw ());
        Assert.Null (exception);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_DoubleStyle_Draw_DoesNotThrow ()
    {
        var top = new Toplevel ();
        var win = new Window { Width = 10, Height = 5, BorderStyle = LineStyle.None };
        top.Add (win);

        var line = new Line { X = 1, Y = 1, Width = 5, Style = LineStyle.Double };
        win.Add (line);

        RunState rs = Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize (new Size (10, 5));
        
        var exception = Record.Exception(() => top.Draw ());
        Assert.Null (exception);

        Application.End (rs);
        top.Dispose ();
    }

    [Fact]
    [AutoInitShutdown]
    public void Line_Intersection_DoesNotThrow ()
    {
        var top = new Toplevel ();
        var win = new Window { Width = 10, Height = 7, BorderStyle = LineStyle.None };
        top.Add (win);

        // Horizontal line
        var hLine = new Line { X = 1, Y = 2, Width = 5, Style = LineStyle.Single };
        // Vertical line intersecting the horizontal
        var vLine = new Line
        {
            X = 3, Y = 1, Height = 3, Orientation = Orientation.Vertical, Style = LineStyle.Single
        };

        win.Add (hLine, vLine);

        RunState rs = Application.Begin (top);
        AutoInitShutdownAttribute.FakeResize (new Size (10, 7));
        
        var exception = Record.Exception(() => top.Draw ());
        Assert.Null (exception);

        Application.End (rs);
        top.Dispose ();
    }
}
