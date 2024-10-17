using Xunit.Abstractions;
using static Terminal.Gui.Pos;

namespace Terminal.Gui.LayoutTests;

public class PosViewTests (ITestOutputHelper output)
{
    private readonly ITestOutputHelper _output = output;

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
    [TestRespondersDisposed]
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

#if DEBUG_IDISPOSABLE

        // HACK: Force clean up of Responders to avoid having to Dispose all the Views created above.
        Responder.Instances.Clear ();
#endif
    }

    [Fact]
    public void PosView_Side_SetToNull_Throws ()
    {
        Assert.Throws<ArgumentNullException> (() => X (null));
        Assert.Throws<ArgumentNullException> (() => Y (null));
        Assert.Throws<ArgumentNullException> (() => Left (null));
        Assert.Throws<ArgumentNullException> (() => Right (null));
        Assert.Throws<ArgumentNullException> (() => Bottom (null));
        Assert.Throws<ArgumentNullException> (() => Top (null));
    }

    // TODO: This actually a SetRelativeLayout/LayoutSubViews test and should be moved
    // TODO: A new test that calls SetRelativeLayout directly is needed.
    [Fact]
    [TestRespondersDisposed]
    public void Subtract_Operator ()
    {
        Application.Init (new FakeDriver ());

        var top = new Toplevel ();

        var view = new View { X = 0, Y = 0, Width = 20, Height = 20 };
        var field = new TextField { X = 0, Y = 0, Width = 20 };
        var count = 20;
        List<View> listViews = new ();

        for (var i = 0; i < count; i++)
        {
            field.Text = $"View {i}";
            var view2 = new View { X = 0, Y = field.Y, Width = 20, Text = field.Text };
            view.Add (view2);
            Assert.Equal ($"View {i}", view2.Text);
            Assert.Equal ($"Absolute({i})", field.Y.ToString ());
            listViews.Add (view2);

            Assert.Equal ($"Absolute({i})", field.Y.ToString ());
            field.Y += 1;
            Assert.Equal ($"Absolute({i + 1})", field.Y.ToString ());
        }

        field.KeyDown += (s, k) =>
                         {
                             if (k.KeyCode == KeyCode.Enter)
                             {
                                 Assert.Equal ($"View {count - 1}", listViews [count - 1].Text);
                                 view.Remove (listViews [count - 1]);
                                 listViews [count - 1].Dispose ();

                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                                 field.Y -= 1;
                                 count--;
                                 Assert.Equal ($"Absolute({count})", field.Y.ToString ());
                             }
                         };

        Application.Iteration += (s, a) =>
                                 {
                                     while (count > 0)
                                     {
                                         field.NewKeyDownEvent (new (KeyCode.Enter));
                                     }

                                     Application.RequestStop ();
                                 };

        var win = new Window ();
        win.Add (view);
        win.Add (field);

        top.Add (win);

        Application.Run (top);
        top.Dispose ();
        Assert.Equal (0, count);

        // Shutdown must be called to safely clean up Application if Init has been called
        Application.Shutdown ();
    }
}
