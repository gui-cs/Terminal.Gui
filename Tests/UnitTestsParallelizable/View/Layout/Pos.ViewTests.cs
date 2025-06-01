using static Terminal.Gui.ViewBase.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosViewTests
{
    [Fact]
    public void PosView_Equal ()
    {
        var view1 = new View ();
        var view2 = new View ();

        Pos pos1 = Left (view1);
        Pos pos2 = Left (view1);
        Assert.Equal (pos1, pos2);

        pos2 = Left (view2);
        Assert.NotEqual (pos1, pos2);

        pos2 = Right (view1);
        Assert.NotEqual (pos1, pos2);
    }

    // TODO: Test Left, Top, Right bottom Equal

    /// <summary>Tests Pos.Left, Pos.X, Pos.Top, Pos.Y, Pos.Right, and Pos.Bottom set operations</summary>
    [Fact]
    public void PosView_Side_SetsValue ()
    {
        string side; // used in format string
        var testRect = Rectangle.Empty;
        var testInt = 0;
        Pos pos;

        // Pos.Left
        side = "Left";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Left (new ());
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        pos = Left (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        testRect = new (1, 2, 3, 4);
        pos = Left (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        // Pos.Left(win) + 0
        pos = Left (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Left(win) +1
        pos = Left (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Left(win) -1
        pos = Left (new () { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.X
        side = "Left";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = X (new ());
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        pos = X (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        testRect = new (1, 2, 3, 4);
        pos = X (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        // Pos.X(win) + 0
        pos = X (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.X(win) +1
        pos = X (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.X(win) -1
        pos = X (new () { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.Top
        side = "Top";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Top (new ());
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        pos = Top (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        testRect = new (1, 2, 3, 4);
        pos = Top (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        // Pos.Top(win) + 0
        pos = Top (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Top(win) +1
        pos = Top (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Top(win) -1
        pos = Top (new () { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.Y
        side = "Top";
        testInt = 0;
        testRect = Rectangle.Empty;
        pos = Y (new ());
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        pos = Y (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        testRect = new (1, 2, 3, 4);
        pos = Y (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        // Pos.Y(win) + 0
        pos = Y (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Y(win) +1
        pos = Y (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Y(win) -1
        pos = Y (new () { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        // Pos.Bottom
        side = "Bottom";
        testRect = Rectangle.Empty;
        testInt = 0;
        pos = Bottom (new ());
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        pos = Bottom (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        testRect = new (1, 2, 3, 4);
        pos = Bottom (new () { Frame = testRect });
        Assert.Equal ($"View(Side={side},Target=View(){testRect})", pos.ToString ());

        // Pos.Bottom(win) + 0
        pos = Bottom (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = 1;

        // Pos.Bottom(win) +1
        pos = Bottom (new () { Frame = testRect }) + testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );

        testInt = -1;

        // Pos.Bottom(win) -1
        pos = Bottom (new () { Frame = testRect }) - testInt;

        Assert.Equal (
                      $"Combine(View(Side={side},Target=View(){testRect}){(testInt < 0 ? '-' : '+')}Absolute({testInt}))",
                      pos.ToString ()
                     );
    }
}
